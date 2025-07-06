# RPC Style Core Library (C# Version) and Orleans-based RPC Core Library Implementation

## Table of Contents

1. [RPC Style Core Library (C# Version)](#rpc-style-core-library-c-version)
   - [Design Objectives and RPC Style Characteristics](#design-objectives-and-rpc-style-characteristics)
   - [Overall Architecture and Ports Abstraction](#overall-architecture-and-ports-abstraction)
   - [Ports and Interface Definitions](#ports-and-interface-definitions)
   - [Core Service Components](#core-service-components)
2. [Orleans-based RPC Core Library Implementation](#orleans-based-rpc-core-library-implementation)
   - [Actor and Core Connection Pattern](#actor-and-core-connection-pattern)
   - [Dependency Injection and Component Assembly](#dependency-injection-and-component-assembly)
   - [Message Routing and Event Forwarding](#message-routing-and-event-forwarding)

---

## RPC Style Core Library (C# Version)

### Design Objectives and RPC Style Characteristics

To support a unified abstraction for RPC-style communication, we designed the core library in C# by using interface definitions to decouple the core business logic from specific frameworks (e.g., Orleans Actor), ensuring high code reusability. The RPC style we focus on includes the following key characteristics:
- Request-response communication
- Point-to-point invocation pattern
- Highly abstracted interface design

### Overall Architecture and Ports Abstraction

The RPC style core library in this project follows the classic **Clean Architecture** and **Ports and Adapters** pattern.

The core library is organized into three main layers:
- **Domain Layer**: Implemented in the `Common/` folder, containing all entities, events, integration models, and requests.
- **Application Layer**: Implemented in `Interfaces/` and `Services/`. `Interfaces/` defines the service interfaces required by the Online Marketplace Benchmark. `Services/` implements these interfaces and contains core business logic (such as order creation and payment).
- **Port Layer**: Implemented in `Ports/`, defining the external dependencies required by the core services, such as order repositories and payment gateways. These are abstractions of technical infrastructure injected externally via dependency injection (DI).

This architecture strictly follows the **Dependency Inversion Principle (DIP)**, where all upper layers depend only on interfaces and never on concrete implementations like databases or message queues. External dependencies are provided through adapters, which are injected into application services via constructor parameters. This design ensures that core business logic is fully decoupled from specific technical implementations, making it easier to replace components in the future.

### Ports and Interface Definitions

The core library defines several interface types (ports), each representing a key abstraction for microservices. These are grouped into three categories: **Repositories**, **Notifiers**, and **Gateways**.

#### Repositories
- **ICartRepository**: Defines storage operations related to the shopping cart, including loading cart status, saving cart changes, and clearing the cart.
- **ICustomerRepository**: Defines storage operations related to user information, including obtaining the user's current status, saving or updating user status, and clearing user information.
- **IProductRepository**: Handles the persistence of product entities.
- **ISellerRepository**: Provides methods for loading, updating, clearing seller data, and managing seller order records.
- **IShipmentRepository**: Manages shipment-related data, including creating shipment records, history queries, deletion, and shipment statistics per seller.
- **IStockRepository**: Defines storage operations for stock items, responsible for persisting inventory data.
- **IOrderRepository**: Supports order creation, saving, retrieval, deletion, querying by customer, and clearing order records.
- **IOrderEntryViewRepository**: Maintains seller-side views of order entries, supporting CRUD operations, view refresh, and local caching.

#### Notifiers
- **ICustomerNotifier**: Defines an asynchronous notification interface used to deliver relevant messages to the customer context after events such as successful payments.
- **IOrderNotifier**: Defines an asynchronous notification interface related to orders, used to route events such as payment success and shipping status changes to the corresponding order context.
- **ISellerNotifier**: Defines an asynchronous notification interface for sellers, responsible for delivering notifications to the seller context when key events like orders, payments, or shipments occur.
- **IStockNotifier**: Defines an asynchronous notification port for the inventory system, routing events to the corresponding stock context when product information (e.g., price, inventory) is updated.

#### Gateways
- **IOrderGateway**: Defines an asynchronous invocation port for the order service. During the checkout phase, it sends a processing request to the order system, triggering stock validation, order creation, seller notification, and payment initiation.
- **IPaymentGateway**: Defines an asynchronous invocation port for the payment process. After an order is created, it sends a payment request to the payment system, triggering payment confirmation, stock update, and event broadcasting.
- **IShipmentGateway**: Defines an asynchronous invocation port for the shipping process. After payment is completed, it initiates warehouse shipment, including parcel splitting, seller notification, and order updates.

### Core Service Components

The core library defines several important service components. Each component is organized around interface-driven business logic, with clear responsibilities and collaboration through RPC-style calls:
- **CartServiceCore**: Responsible for managing cart data and state. It depends on `ICartRepository` and `IOrderGateway`, with main responsibilities including creating, adding items, checking out, and clearing the cart.
- **CustomerServiceCore**: Manages customer aggregates. It depends on `ICustomerRepository`, managing the current customer object’s state (setting, updating, clearing), responding to key business events.
- **OrderServiceCore**: Manages order aggregates, encapsulating the full lifecycle from order creation to delivery. It depends on `IOrderRepository`, `IStockReserver`, `ISellerNotifier`, and `IPaymentGateway`.
- **PaymentServiceCore**: Coordinates payment processing. It depends on `IStockGateway`, `ISellerNotifier`, `IOrderNotifier`, `ICustomerNotifier`, and `IShipmentGateway`, constructing payment confirmation events and broadcasting payment results.
- **ProductServiceCore**: Manages product data. It depends on `IProductRepository`, `IReplicationPublisher`, and `IStockNotifier`, responsible for setting and updating product information, ensuring data consistency.
- **SellerServiceCore**: Manages seller-side order aggregation and views. It depends on `ISellerRepository`, recording order details, handling payment and shipment notifications.
- **ShipmentServiceCore**: Manages the shipment and delivery lifecycle. It depends on `IShipmentRepository`, `ISellerNotifier`, and `IOrderNotifier`.
- **StockServiceCore**: Manages product inventory status. It depends on `IStockRepository`, supporting inventory queries and reservations, ensuring stock consistency during checkout.

---

## Orleans-based RPC Core Library Implementation

### Actor and Core Connection Pattern

Each **Grain** acts as a host and delegate for the core service class. For example, `OrderActor` holds an instance of `OrderServiceCore`, and all business logic is delegated to it:

```csharp
public class OrderActor : Grain, IOrderActor {
    private OrderServiceCore _svc;

    public override Task OnActivateAsync() {
        _svc = new OrderServiceCore(...);
        return Task.CompletedTask;
    }

    public Task NotifyCheckout(CustomerCheckout cc) => _svc.NotifyCheckoutAsync(cc);
}
```

### Dependency Injection and Component Assembly

Components are assembled in a hybrid manner:

- **Manually** inside `OnActivateAsync()` for scoped objects like state wrappers.
- **Automatically** via Orleans DI container for shared services like `ILogger<T>` or configuration classes.

```csharp
var repo = new OrleansCartRepository(_state);
var order = new OrderGrainGateway(GrainFactory);
var clock = SystemClock.Instance;
_svc = new CartServiceCore(id, repo, order, clock, _log, trackHistory);
```

### Message Routing and Event Forwarding

#### Notifier:

Implements Port interfaces like `IStockNotifier`, receiving events and forwarding them to the corresponding Grain:

```csharp
public sealed class StockGrainNotifier : IStockNotifier {
    public Task NotifyProductUpdated(ProductUpdated evt) {
        var grain = _factory.GetGrain<IStockActor>(evt.sellerId, evt.productId.ToString());
        return grain.ProcessProductUpdate(evt);
    }
}
```

#### Gateway:

Allows Core logic to initiate cross-service calls, handling communication via Grains:

```csharp
public class OrderGrainGateway : IOrderGateway {
    public Task CheckoutAsync(ReserveStock rs) =>
        _gf.GetGrain<IOrderActor>(rs.customerCheckout.CustomerId).Checkout(rs);
}
```

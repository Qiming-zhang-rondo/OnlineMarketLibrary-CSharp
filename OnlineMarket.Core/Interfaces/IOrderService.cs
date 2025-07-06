using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Interfaces;

public interface IOrderService
{
    /// Places an order using the provided stock reservation details.
    Task Checkout(ReserveStock reserveStock);

    /// Handles the event when a payment is confirmed successfully.
    Task ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed);

    /// Handles the event when a payment fails.
    Task ProcessPaymentFailed(PaymentFailed paymentFailed);

    /// Processes a shipment notification event.
    Task ProcessShipmentNotification(ShipmentNotification shipmentNotification);

    /// Retrieves the list of all current orders.
    Task<List<Order>> GetOrders();

    /// Returns the total number of orders.
    Task<int> GetNumOrders();

    /// Resets the order state (typically used for testing or reinitialization).
    Task Reset();
}
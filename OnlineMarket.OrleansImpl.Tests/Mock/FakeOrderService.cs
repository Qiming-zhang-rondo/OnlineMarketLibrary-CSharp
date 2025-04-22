using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Interfaces;

public class FakeOrderService : IOrderService
{
    public Task Checkout(ReserveStock reserveStock)
    {
        return Task.CompletedTask;
    }

    public Task ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed)
    {
        return Task.CompletedTask;
    }

    public Task ProcessPaymentFailed(PaymentFailed paymentFailed)
    {
        return Task.CompletedTask;
    }

    public Task ProcessShipmentNotification(ShipmentNotification shipmentNotification)
    {
        return Task.CompletedTask;
    }

    public Task<List<Order>> GetOrders(long customerId)
    {
        return Task.FromResult(new List<Order>());
    }

    public Task<int> GetNumOrders(long customerId)
    {
        return Task.FromResult(0);
    }

    public Task Reset(long customerId)
    {
        return Task.CompletedTask;
    }

    public Task<List<Order>> GetOrders()
    {
        throw new NotImplementedException();
    }

    public Task<int> GetNumOrders()
    {
        throw new NotImplementedException();
    }

    public Task Reset()
    {
        throw new NotImplementedException();
    }
}
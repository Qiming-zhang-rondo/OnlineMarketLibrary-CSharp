using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Interfaces;

public interface IOrderService
{
    Task Checkout(ReserveStock reserveStock);
    Task ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed);
    Task ProcessPaymentFailed(PaymentFailed paymentFailed);
    Task ProcessShipmentNotification(ShipmentNotification shipmentNotification);
    Task<List<Order>> GetOrders();
    Task<int> GetNumOrders();
    Task Reset();
}
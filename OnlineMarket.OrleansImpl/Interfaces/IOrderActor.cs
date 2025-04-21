using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineMarket.OrleansImpl.Interfaces
{
    public interface IOrderActor : IGrainWithIntegerKey
    {
        Task Checkout(ReserveStock reserveStock);
        Task ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed);
        Task ProcessPaymentFailed(PaymentFailed paymentFailed);
        Task ProcessShipmentNotification(ShipmentNotification shipmentNotification);
        Task<List<Order>> GetOrders();
        Task<int> GetNumOrders();
        Task Reset();
    }
}
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public class OrderActorAdapter : IOrderService
    {
        private readonly int customerId;
        private readonly IGrainFactory grainFactory;

        public OrderActorAdapter(int customerId, IGrainFactory grainFactory)
        {
            this.customerId = customerId;
            this.grainFactory = grainFactory;
        }

        private IOrderActor GetOrderActor()
        {
            return grainFactory.GetGrain<IOrderActor>(customerId);
        }

        public Task Checkout(ReserveStock reserveStock)
        {
            return GetOrderActor().Checkout(reserveStock);
        }

        public Task ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed)
        {
            return GetOrderActor().ProcessPaymentConfirmed(paymentConfirmed);
        }

        public Task ProcessPaymentFailed(PaymentFailed paymentFailed)
        {
            return GetOrderActor().ProcessPaymentFailed(paymentFailed);
        }

        public Task ProcessShipmentNotification(ShipmentNotification shipmentNotification)
        {
            return GetOrderActor().ProcessShipmentNotification(shipmentNotification);
        }

        public Task<List<Order>> GetOrders()
        {
            return GetOrderActor().GetOrders();
        }

        public Task<int> GetNumOrders()
        {
            return GetOrderActor().GetNumOrders();
        }

        public Task Reset()
        {
            return GetOrderActor().Reset();
        }
    }
}
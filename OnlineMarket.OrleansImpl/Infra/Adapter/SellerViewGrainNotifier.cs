using System.Threading.Tasks;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans;
using OnlineMarket.Core.Common.Entities;


namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    
    // Forwards notifications to the SellerViewActor based on the Postgres view.
    public sealed class SellerViewGrainNotifier : ISellerNotifier
    {
        private readonly IGrainFactory _factory;
        public SellerViewGrainNotifier(IGrainFactory factory) => _factory = factory;
        
        private ISellerActor GetSeller(int sellerId) => _factory.GetGrain<ISellerActor>(sellerId);

        private ISellerViewActor GetSellerView(int sellerId) =>
            _factory.GetGrain<ISellerViewActor>(sellerId);

        public Task NotifyShipment(ShipmentNotification n)
        {
            if (n.SellerId <= 0) return Task.CompletedTask;
            return GetSellerView(n.SellerId).ProcessShipmentNotification(n);
        }

        public Task NotifyDelivery(DeliveryNotification n)
        {
            return GetSellerView(n.sellerId).ProcessDeliveryNotification(n);
        }
        
        public async Task NotifyInvoiceAsync(InvoiceIssued v)
        {
            // items → get seller_id → convert to Grain → remove duplicates → call one by one
            foreach (var g in v.items
                         .Select<OrderItem, ISellerActor>(i => GetSeller(i.seller_id))
                         .Distinct())
            {
                await g.ProcessNewInvoice(v);
            }

        }
        public Task NotifyPaymentConfirmedAsync(PaymentConfirmed v) =>
            Task.WhenAll(v.items.Select(i => GetSeller(i.seller_id).ProcessPaymentConfirmed(v)));
    }
}
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;
using System.Linq;
using OnlineMarket.Core.Common.Entities;


namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    /// <summary>
    /// 把通知转发给传统 SellerActor。
    /// </summary>
    public sealed class SellerGrainNotifier : ISellerNotifier
    {
        private readonly IGrainFactory _factory;
        public SellerGrainNotifier(IGrainFactory factory) => _factory = factory;

        private ISellerActor GetSeller(int sellerId) => _factory.GetGrain<ISellerActor>(sellerId);

        public Task NotifyShipment(ShipmentNotification n)
        {
            // SellerId 已经包含在事件对象里
            if (n.SellerId <= 0) return Task.CompletedTask;
            return GetSeller(n.SellerId).ProcessShipmentNotification(n);
        }

        public Task NotifyDelivery(DeliveryNotification n)
        {
            return GetSeller(n.sellerId).ProcessDeliveryNotification(n);
        }
        
        public async Task NotifyInvoiceAsync(InvoiceIssued v)
        {
            // items → 取 seller_id → 转成 Grain → 去重 → 逐个调用
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
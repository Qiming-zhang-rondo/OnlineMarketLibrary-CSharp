using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public class SellerActorAdapter : ISellerService
    {
        private readonly int sellerId;
        private readonly IGrainFactory grainFactory;

        public SellerActorAdapter(int sellerId, IGrainFactory grainFactory)
        {
            this.sellerId = sellerId;
            this.grainFactory = grainFactory;
        }

        private ISellerActor GetSellerActor()
        {
            return grainFactory.GetGrain<ISellerActor>(sellerId);
        }

        public Task ProcessNewInvoice(InvoiceIssued invoiceIssued)
        {
            return GetSellerActor().ProcessNewInvoice(invoiceIssued);
        }

        public Task ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed)
        {
            return GetSellerActor().ProcessPaymentConfirmed(paymentConfirmed);
        }

        public Task ProcessPaymentFailed(PaymentFailed paymentFailed)
        {
            return GetSellerActor().ProcessPaymentFailed(paymentFailed);
        }

        public Task ProcessShipmentNotification(ShipmentNotification shipmentNotification)
        {
            return GetSellerActor().ProcessShipmentNotification(shipmentNotification);
        }

        public Task ProcessDeliveryNotification(DeliveryNotification deliveryNotification)
        {
            return GetSellerActor().ProcessDeliveryNotification(deliveryNotification);
        }

        public Task SetSeller(Seller seller)
        {
            return GetSellerActor().SetSeller(seller);
        }

        public Task<Seller> GetSeller()
        {
            return GetSellerActor().GetSeller();
        }

        public Task<SellerDashboard> QueryDashboard()
        {
            return GetSellerActor().QueryDashboard();
        }

        public Task Reset()
        {
            return GetSellerActor().Reset();
        }
    }
}
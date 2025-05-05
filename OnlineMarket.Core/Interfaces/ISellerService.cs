using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.Core.Interfaces
{
    public interface ISellerService
    {
        Task ProcessNewInvoice(InvoiceIssued invoiceIssued);

        Task ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed);

        Task ProcessPaymentFailed(PaymentFailed paymentFailed);

        Task ProcessShipmentNotification(ShipmentNotification shipmentNotification);

        Task ProcessDeliveryNotification(DeliveryNotification deliveryNotification);

        Task SetSeller(Seller seller);

        Task<Seller> GetSeller();

        Task<SellerDashboard> QueryDashboard();

        Task Reset();
    }
}
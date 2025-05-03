using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.Core.Interfaces;

// OnlineMarket.Core.Interfaces/ISellerService.cs
public interface ISellerService
{
    Task SetSeller(Seller seller);
    Task<Seller?> GetSeller();
    Task ProcessNewInvoice(InvoiceIssued evt);
    Task ProcessPaymentConfirmed(PaymentConfirmed evt);
    Task ProcessPaymentFailed(PaymentFailed evt);
    Task ProcessShipmentNotification(ShipmentNotification evt);
    Task ProcessDeliveryNotification(DeliveryNotification evt);
    Task<SellerDashboard> QueryDashboard();
    Task Reset();
}

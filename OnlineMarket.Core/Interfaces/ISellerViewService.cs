using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.Core.Interfaces;

// Interfaces/ISellerViewService.cs
public interface ISellerViewService
{
    Task SetSeller(Seller seller);
    Task<Seller> GetSeller();

    Task ProcessNewInvoice(InvoiceIssued e);
    Task ProcessPaymentConfirmed(PaymentConfirmed e);    // 这里两个方法仍空实现，可留着将来用
    Task ProcessPaymentFailed(PaymentFailed e);

    Task ProcessShipmentNotification(ShipmentNotification e);
    Task ProcessDeliveryNotification(DeliveryNotification e);

    Task<SellerDashboard> QueryDashboard();
    Task Reset();
}

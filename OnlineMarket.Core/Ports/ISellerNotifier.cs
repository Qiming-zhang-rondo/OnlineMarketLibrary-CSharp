using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Ports;

public interface ISellerNotifier
{
    //shipment
    Task NotifyShipment(ShipmentNotification n);
    Task NotifyDelivery(DeliveryNotification n);
    //Order
    Task NotifyInvoiceAsync(InvoiceIssued v);           // ★ 新增
    //Payment
    Task NotifyPaymentConfirmedAsync(PaymentConfirmed v);
}
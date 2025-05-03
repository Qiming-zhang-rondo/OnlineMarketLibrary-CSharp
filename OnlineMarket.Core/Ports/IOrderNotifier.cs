using OnlineMarket.Core.Common.Events;
namespace OnlineMarket.Core.Ports;

public interface IOrderNotifier
{
    //shipment
    Task NotifyShipment(ShipmentNotification n);
    //payment
    Task NotifyPaymentAsync(PaymentConfirmed v);
}
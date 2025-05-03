using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Ports;

public interface IShipmentGateway
{
    Task StartShipmentAsync(PaymentConfirmed v);
}
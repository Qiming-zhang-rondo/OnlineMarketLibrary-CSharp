using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Ports;

public interface ICustomerNotifier
{
    Task NotifyPaymentAsync(PaymentConfirmed v);
}
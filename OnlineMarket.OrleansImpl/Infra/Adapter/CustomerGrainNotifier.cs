using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

public sealed class CustomerGrainNotifier : ICustomerNotifier
{
    private readonly IGrainFactory _gf;
    public CustomerGrainNotifier(IGrainFactory gf) => _gf = gf;
    public Task NotifyPaymentAsync(PaymentConfirmed v) =>
        _gf.GetGrain<ICustomerActor>(v.customer.CustomerId)
            .NotifyPaymentConfirmed(v);
}
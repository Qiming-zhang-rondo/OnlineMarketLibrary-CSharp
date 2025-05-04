using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

/* PaymentGrainGateway : IPaymentGateway */
class PaymentGrainGateway : IPaymentGateway
{
    private readonly IGrainFactory _gf;
    public PaymentGrainGateway(IGrainFactory gf) => _gf = gf;
    public Task StartPaymentAsync(InvoiceIssued v)
    {
        return _gf.GetGrain<IPaymentActor>(v.customer.CustomerId).ProcessPayment(v);
    }
}
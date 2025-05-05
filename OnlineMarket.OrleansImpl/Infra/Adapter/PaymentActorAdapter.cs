using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public sealed class PaymentActorAdapter : IPaymentService
    {
        private readonly int customerId;
        private readonly IGrainFactory grainFactory;

        public PaymentActorAdapter(int customerId, IGrainFactory grainFactory)
        {
            this.customerId = customerId;
            this.grainFactory = grainFactory;
        }

        private IPaymentActor GetPaymentActor()
        {
            return grainFactory.GetGrain<IPaymentActor>(customerId);
        }

        public Task ProcessPayment(InvoiceIssued invoiceIssued)
        {
            return GetPaymentActor().ProcessPayment(invoiceIssued);
        }
    }
}
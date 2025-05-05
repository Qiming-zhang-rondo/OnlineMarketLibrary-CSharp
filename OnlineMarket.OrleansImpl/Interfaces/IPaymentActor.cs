using OnlineMarket.Core.Common.Events;


namespace OnlineMarket.OrleansImpl.Interfaces
{
    public interface IPaymentActor : IGrainWithIntegerKey
    {
        Task ProcessPayment(InvoiceIssued invoiceIssued);
    }
}
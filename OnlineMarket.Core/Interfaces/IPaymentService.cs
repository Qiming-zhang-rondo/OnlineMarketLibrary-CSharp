using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Interfaces
{
    public interface IPaymentService
    {
        Task ProcessPayment(InvoiceIssued invoiceIssued);
    }
}
// OnlineMarket.Core.Interfaces/IPaymentService.cs

using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Interfaces
{
    public interface IPaymentService
    {
        Task ProcessPaymentAsync(InvoiceIssued invoice);
    }
}
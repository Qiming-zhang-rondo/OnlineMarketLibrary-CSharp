using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.OrleansImpl.Interfaces;

// 带事务版本
public interface ITransactionalPaymentActor : IPaymentActor
{
    [Transaction(TransactionOption.Join)]
    new Task ProcessPayment(InvoiceIssued invoice);
}
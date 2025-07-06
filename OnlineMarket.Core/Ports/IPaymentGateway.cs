using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Ports;

public interface IPaymentGateway
{
    Task StartPaymentAsync(InvoiceIssued v);
}
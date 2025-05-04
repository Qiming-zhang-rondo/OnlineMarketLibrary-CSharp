// OnlineMarket.Core.Ports/IOrderRepository.cs

using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Ports;

public interface IOrderRepository
{
    Task<int>  GetNextIdAsync();
    Task SaveAsync(int id, Order order,
        List<OrderItem> items, List<OrderHistory> hist);
    Task<(Order,List<OrderItem>,List<OrderHistory>)> LoadAsync(int id);
    Task DeleteAsync(int id);
    Task<List<Order>> QueryByCustomerAsync(int customerId);
    Task<int>  CountAsync();
    Task ResetAsync(int customerId);
}

// 外部交互
public interface IPaymentGateway{ Task StartPaymentAsync(InvoiceIssued v); }
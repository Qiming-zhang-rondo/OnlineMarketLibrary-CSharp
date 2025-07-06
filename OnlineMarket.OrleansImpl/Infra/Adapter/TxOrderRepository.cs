// using OnlineMarket.Core.Common.Entities;
// using OnlineMarket.Core.Ports;
// using Orleans.Transactions.Abstractions;
//
// namespace OnlineMarket.OrleansImpl.Infra.Adapter;
//
// /*──────────────────── 内部：Tx‑Repository 适配 ───────────────*/
// public sealed class TxOrderRepository : IOrderRepository
// {
//     private readonly ITransactionalState<Dictionary<int, OrderGrainState>> _orders;
//     private readonly ITransactionalState<OrderIdCounter>                   _id;
//
//     public TxOrderRepository(
//         ITransactionalState<Dictionary<int, OrderGrainState>> orders,
//         ITransactionalState<OrderIdCounter> id)
//     { _orders = orders; _id = id; }
//
//     /*―――― 自增主键 ――――*/
//     public async Task<int> GetNextIdAsync()
//     {
//         // 调用 Counter.GetNext() 返回新的 Counter，再把它当作新状态
//         var newState = await _id.PerformUpdate(c => c.GetNext());
//         return newState.Value;
//     }
//
//     /*―――― CRUD (全部包在调用方事务内) ――――*/
//     public Task SaveAsync(int id, Order order, List<OrderItem> it, List<OrderHistory> hist)
//         => _orders.PerformUpdate(d => {
//             d[id] = new OrderGrainState {
//                 Order   = order,
//                 Items   = it,
//                 History = hist
//             };
//             return d;
//         });
//
//
//     public Task<(Order, List<OrderItem>, List<OrderHistory>)> LoadAsync(int id)
//         => _orders.PerformRead<(Order, List<OrderItem>, List<OrderHistory>)>(d => {
//             if (d.TryGetValue(id, out var s))
//                 return (s.Order, s.Items, s.History);
//             else
//                 return (null!, new(), new());
//         });
//
//
//     public Task DeleteAsync(int id)
//         => _orders.PerformUpdate(d => d.Remove(id));
//
//     public Task<List<Order>> QueryByCustomerAsync(int customerId)
//     {
//         // 显式指定 PerformRead 的 TResult 为 List<Order>
//         return _orders.PerformRead<List<Order>>(state =>
//         {
//             // state.Values 是 OrderGrainState 的集合
//             // 取出其中的 .Order 属性，再过滤 customer_id，最后 ToList()
//             return state.Values
//                 .Select((OrderGrainState s) => s.Order)
//                 .Where(o => o.customer_id == customerId)
//                 .ToList();
//         });
//     }
//
//     public Task<int> CountAsync()
//         => _orders.PerformRead(d => d.Count);
//
//     public Task ResetAsync(int _)
//         => _orders.PerformUpdate(d => d.Clear());
// }
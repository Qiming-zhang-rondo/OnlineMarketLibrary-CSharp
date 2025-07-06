// OnlineMarket.OrleansImpl.Infra.Adapter/OrleansOrderRepository.cs
using System.Linq;
using System.Threading.Tasks;
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Common.Entities;
using Orleans.Runtime;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

sealed class OrleansOrderRepository : IOrderRepository
{
    private readonly IPersistentState<Dictionary<int,OrderGrainState>> _state;
    private readonly IPersistentState<OrderIdCounter>                  _id;

    public OrleansOrderRepository(
        IPersistentState<Dictionary<int,OrderGrainState>> state,
        IPersistentState<OrderIdCounter> id)
    { _state = state; _id = id; }

    /*──────── IOrderRepository  ───────*/

    public Task<int> GetNextIdAsync()
    {
        _id.State = _id.State?.GetNext() ?? new OrderIdCounter{ Value = 1 };
        return Task.FromResult(_id.State.Value);
    }

    public async Task SaveAsync(int id, Order o, List<OrderItem> it, List<OrderHistory> h)
    {
        _state.State[id] = new OrderGrainState { Order = o, Items = it, History = h };
        await Task.WhenAll(_state.WriteStateAsync(), _id.WriteStateAsync());
    }

    public Task<(Order,List<OrderItem>,List<OrderHistory>)> LoadAsync(int id)
    {
        var s = _state.State[id];
        return Task.FromResult((s.Order, s.Items, s.History));
    }

    public async Task DeleteAsync(int id)
    {
        _state.State.Remove(id);
        await _state.WriteStateAsync();
    }

    public Task<List<Order>> QueryByCustomerAsync(int cid) =>
        Task.FromResult(_state.State.Values
            .Select(v => v.Order)
            .Where(o => o.customer_id == cid)
            .ToList());

    public Task<int> CountAsync() => Task.FromResult(_state.State.Count);

    public Task ResetAsync(int _) => DeleteAll();

    /*──────── Private ───────*/
    private Task DeleteAll()
    {
        _state.State.Clear();
        return _state.WriteStateAsync();
    }
}
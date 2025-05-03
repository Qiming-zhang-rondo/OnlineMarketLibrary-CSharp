using OnlineMarket.Core.Ports;
using Orleans.Runtime;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

// Adapter/OrleansShipmentRepository.cs
//新增NextShipmentIdState Entities
sealed class OrleansShipmentRepository : IShipmentRepository
{
    private readonly IPersistentState<SortedDictionary<int,Shipment>> _ship;
    private readonly IPersistentState<SortedDictionary<int,List<Package>>> _pkg;
    private readonly IPersistentState<NextShipmentIdState> _id;

    public OrleansShipmentRepository(
        IPersistentState<SortedDictionary<int,Shipment>> ship,
        IPersistentState<SortedDictionary<int,List<Package>>> pkg,
        IPersistentState<NextShipmentIdState> id)
    {
        _ship = ship; _pkg = pkg; _id = id;
    }

    public Task<int> GetNextIdAsync() => Task.FromResult(_id.State.GetNextShipmentId().Value);

    public async Task SaveAsync(int id, Shipment s, List<Package> p)
    {
        _ship.State[id] = s;
        _pkg.State[id]  = p;
        await Task.WhenAll(_ship.WriteStateAsync(), _pkg.WriteStateAsync(), _id.WriteStateAsync());
    }

    public Task<(Shipment,List<Package>)> LoadAsync(int id) =>
        Task.FromResult((_ship.State[id], _pkg.State[id]));

    public async Task DeleteAsync(int id)
    {
        _ship.State.Remove(id);
        _pkg.State.Remove(id);
        await Task.WhenAll(_ship.WriteStateAsync(), _pkg.WriteStateAsync());
    }

    public Task<List<Shipment>> QueryByCustomerAsync(int cid) =>
        Task.FromResult(_ship.State.Values.Where(s => s.customer_id == cid).ToList());

    public Task<Dictionary<int,int>> OldestOpenPerSellerAsync(int take) =>
        Task.FromResult(
            _pkg.State.Take(take)
                .SelectMany(kv => kv.Value)
                .GroupBy(p => p.seller_id)
                .ToDictionary(g => g.Key, g => g.Min(x => x.shipment_id)));

    public Task ResetAsync()
    {
        _ship.State.Clear();
        _pkg.State.Clear();
        return Task.WhenAll(_ship.WriteStateAsync(), _pkg.WriteStateAsync());
    }
}

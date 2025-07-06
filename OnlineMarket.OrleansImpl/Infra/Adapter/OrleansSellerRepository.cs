using OnlineMarket.Core.Ports;
using Orleans.Runtime;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

public sealed class OrleansSellerRepository : ISellerRepository
{
    private readonly IPersistentState<Seller> _seller;
    private readonly IPersistentState<Dictionary<string,List<OrderEntry>>> _entries;

    public OrleansSellerRepository(
        IPersistentState<Seller> seller,
        IPersistentState<Dictionary<string,List<OrderEntry>>> entries)
    { _seller = seller; _entries = entries; }

    public Task<Seller?> LoadSellerAsync(int _) => Task.FromResult(_seller.State);

    public Task SaveSellerAsync(Seller s)
    {
        _seller.State = s;
        return _seller.WriteStateAsync();
    }

    public Task<IDictionary<string,List<OrderEntry>>> LoadEntriesAsync(int _)
        => Task.FromResult((IDictionary<string,List<OrderEntry>>)_entries.State);

    public Task SaveEntriesAsync(IDictionary<string,List<OrderEntry>> d)
    {
        // _entries.State = new(d);
        // return _entries.WriteStateAsync();
        _entries.State.Clear();                // 保留引用
        foreach (var kv in d)
            _entries.State[kv.Key] = kv.Value;
        return _entries.WriteStateAsync();
    }

    public async Task ResetAsync(int _)
    {
        
        await _entries.ReadStateAsync();       // Pull the latest ETag first
        // ① Do not create a new dictionary, just Clear the reference → ETag remains unchanged
        _entries.State.Clear();

        // ② The same is true for seller, keep the reference and only set the field or attribute to empty
        _seller.State = null;
        
        // ③ Write back to storage
        await Task.WhenAll(_entries.WriteStateAsync(),
            _seller.WriteStateAsync());
        // return Task.WhenAll(
        //     _seller.WriteStateAsync(),
        //     _entries.WriteStateAsync());
    }
}

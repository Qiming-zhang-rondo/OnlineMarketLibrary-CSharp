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
        
        await _entries.ReadStateAsync();       // 先拉最新 ETag
        // ① 不要 new 新字典，直接 Clear 保留引用 → ETag 不变
        _entries.State.Clear();

        // ② seller 也是同理，保留引用，仅置空字段或属性
        _seller.State = null;
        
        // ③ 写回存储
        await Task.WhenAll(_entries.WriteStateAsync(),
            _seller.WriteStateAsync());
        // return Task.WhenAll(
        //     _seller.WriteStateAsync(),
        //     _entries.WriteStateAsync());
    }
}

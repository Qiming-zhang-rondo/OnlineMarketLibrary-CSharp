using Microsoft.EntityFrameworkCore;
using OnlineMarket.Core.Ports;
using Orleans.Runtime;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.OrleansImpl.Infra.SellerDb;


namespace OnlineMarket.OrleansImpl.Infra.Adapter;

sealed class OrleansSellerViewRepository : IOrderEntryViewRepository
{
    private readonly IPersistentState<Dictionary<(int,int),List<int>>> _cacheState;
    private readonly IDbContextFactory<SellerDbContext> _factory;

    public OrleansSellerViewRepository(
        IPersistentState<Dictionary<(int,int),List<int>>> cacheState,
        IDbContextFactory<SellerDbContext> factory)
    {
        _cacheState = cacheState;
        _factory = factory;
        // _cacheState.State ??= new();
    }

    public Task<IDictionary<(int,int),List<int>>> LoadCacheAsync(int _)
        => Task.FromResult((IDictionary<(int,int),List<int>>)_cacheState.State);

    public Task SaveCacheAsync(IDictionary<(int,int),List<int>> c)
    {
        _cacheState.State = new(c);
        return _cacheState.WriteStateAsync();
    }

    public async Task AddEntriesAsync(IEnumerable<OrderEntry> entries)
    {
        await using var db = _factory.CreateDbContext();
        db.OrderEntries.AddRange(entries);
        await db.SaveChangesAsync();
    }

    public async Task UpdateEntriesAsync(IEnumerable<OrderEntry> entries)
    {
        await using var db = _factory.CreateDbContext();
        db.OrderEntries.UpdateRange(entries);
        await db.SaveChangesAsync();
    }

    public async Task<IList<OrderEntry>> QueryEntriesBySellerAsync(int sellerId)
    {
        await using var db = _factory.CreateDbContext();
        return await db.OrderEntries
            .Where(e => e.seller_id == sellerId)
            .AsNoTracking()
            .ToListAsync();
    }


    public Task ResetAsync(int _)
    {
        _cacheState.State.Clear();
        return _cacheState.WriteStateAsync();
    }
}

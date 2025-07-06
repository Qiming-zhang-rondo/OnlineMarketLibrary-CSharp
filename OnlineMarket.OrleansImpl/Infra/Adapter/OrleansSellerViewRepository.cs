using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Ports;
using Orleans.Runtime;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.OrleansImpl.Infra.SellerDb;


namespace OnlineMarket.OrleansImpl.Infra.Adapter;

sealed class OrleansSellerViewRepository : IOrderEntryViewRepository
{
    private readonly IPersistentState<Dictionary<(int,int),List<int>>> _cacheState;
    private readonly IDbContextFactory<SellerDbContext> _factory;
    private readonly ILogger<OrleansSellerViewRepository> _logger;


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
        // await using var db = _factory.CreateDbContext();
        // Console.WriteLine(">>> ConnStr = " 
        //                   + db.Database.GetDbConnection().ConnectionString);
        // db.OrderEntries.AddRange(entries);
        // await db.SaveChangesAsync();
        // await using var db = await _factory.CreateDbContextAsync(); 
        await using var db = _factory.CreateDbContext();
        // Print it out to make sure it is connected to the expected library.
        Console.WriteLine(">>> ConnStr = " 
                          + db.Database.GetDbConnection().ConnectionString);
        
        // Insert
        db.OrderEntries.AddRange(entries);
        int n;
        try
        {
            n = await db.SaveChangesAsync();
            Console.WriteLine($">>> Successfully wrote {n} lines");
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> SaveChangesAsync threw an exception: {ex}");
            throw;
        }
        
        var justNow = await db.OrderEntries
            .Where(e => entries.Select(x => x.natural_key).Contains(e.natural_key))
            .ToListAsync();
        Console.WriteLine($">>> Actual number of rows written = {justNow.Count}");
    }
    
    public async Task UpdateEntriesAsync(
        IEnumerable<OrderEntry> entries,
        params Expression<Func<OrderEntry, object>>[] propertiesToUpdate)
    {
        await using var db = _factory.CreateDbContext();
        foreach (var entry in entries)
        {
            db.Attach(entry);
            foreach (var prop in propertiesToUpdate)
            {
                db.Entry(entry).Property(prop).IsModified = true;
            }
        }
        await db.SaveChangesAsync();
    }
    
    
    public async Task DeleteEntriesAsync(IEnumerable<int> ids)
    {
        await using var db = _factory.CreateDbContext();

        // Construct a list of entities with only primary keys, then call RemoveRange
        var toDelete = ids.Select(id => new OrderEntry { id = id }).ToList();
        db.OrderEntries.RemoveRange(toDelete);

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

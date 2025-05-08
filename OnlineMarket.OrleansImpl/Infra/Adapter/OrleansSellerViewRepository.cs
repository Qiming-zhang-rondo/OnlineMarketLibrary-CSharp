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
        // 打印一下，确保连到期望的库
        Console.WriteLine(">>> ConnStr = " 
                          + db.Database.GetDbConnection().ConnectionString);
        
        // 插入
        db.OrderEntries.AddRange(entries);
        int n;
        try
        {
            n = await db.SaveChangesAsync();
            Console.WriteLine($">>> 成功写入 {n} 行");
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> SaveChangesAsync 抛出了异常：{ex}");
            throw;
        }
        
        // **立即查询验证**
        var justNow = await db.OrderEntries
            .Where(e => entries.Select(x => x.natural_key).Contains(e.natural_key))
            .ToListAsync();
        Console.WriteLine($">>> 实际写入行数 = {justNow.Count}");
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

    // public async Task UpdateEntriesAsync(IEnumerable<OrderEntry> entries)
    // {
    //     // 同样，用同步或异步工厂均可，保持一致即可
    //     await using var db = _factory.CreateDbContext();
    //
    //     // 打印连接串，确认是期望的库
    //     Console.WriteLine(">>> ConnStr = " 
    //                       + db.Database.GetDbConnection().ConnectionString);
    //
    //     // 批量更新
    //     // db.OrderEntries.UpdateRange(entries)
    //     foreach (var e in entries)
    //     {
    //         // 把实体附加到上下文，EF 会把它当成 Unchanged 状态
    //         db.Attach(e);
    //
    //         // 标记要修改的属性为 Modified
    //         db.Entry(e).Property(x => x.order_status)   .IsModified = true;
    //         db.Entry(e).Property(x => x.shipment_date)  .IsModified = true;
    //         db.Entry(e).Property(x => x.delivery_status).IsModified = true;
    //         
    //     }
    //
    //     await db.SaveChangesAsync();;
    //     
    //
    //     int n;
    //     try
    //     {
    //         n = await db.SaveChangesAsync();
    //         Console.WriteLine($">>> UpdateEntriesAsync 成功更新 {n} 行");
    //     }
    //     catch (DbUpdateException dbEx)
    //     {
    //         // 打印外层异常消息
    //         Console.WriteLine($">>> DbUpdateException: {dbEx.Message}");
    //         // 如果有 InnerException，就打印更详细的底层数据库异常
    //         if (dbEx.InnerException != null)
    //         {
    //             Console.WriteLine($">>> InnerException: {dbEx.InnerException.GetType().Name} — {dbEx.InnerException.Message}");
    //         }
    //         throw;
    //     }
    //
    //     // 立即查询，验证更新确实生效
    //     // 假设 entries 有 id 列，你可以按 id 去查最新状态
    //     var ids = entries.Select(e => e.id).ToList();
    //     var refreshed = await db.OrderEntries
    //         .Where(e => ids.Contains(e.id))
    //         .AsNoTracking()
    //         .ToListAsync();
    //
    //     Console.WriteLine($">>> 实际查询到 {refreshed.Count} 条被更新的记录");
    //     foreach (var e in refreshed)
    //     {
    //         Console.WriteLine($">>> 记录 id={e.id} 的状态：order_status={e.order_status}, delivery_status={e.delivery_status}");
    //     }
    // }
    
    public async Task DeleteEntriesAsync(IEnumerable<int> ids)
    {
        await using var db = _factory.CreateDbContext();

        // 构造一个只有主键的实体列表，然后调用 RemoveRange
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

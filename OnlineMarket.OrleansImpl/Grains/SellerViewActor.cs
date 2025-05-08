using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Infra.SellerDb;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace OnlineMarket.OrleansImpl.Grains;

// OnlineMarket.OrleansImpl.Grains/SellerViewActor.cs
// [Reentrant]
public sealed class SellerViewActor : Grain, ISellerViewActor
{
    
    private readonly IPersistentState<Seller> _seller;

    
    private readonly IPersistentState<Dictionary<(int,int),List<int>>> _cache;

    private readonly IDbContextFactory<SellerDbContext> _dbFactory;
    private readonly AppConfig   _cfg;
    private readonly IAuditLogger _audit;
    private readonly ILogger<SellerViewServiceCore> _log;
    
    private SellerViewServiceCore _svc = null!;

    public SellerViewActor(IDbContextFactory<SellerDbContext> dbFactory,
        [PersistentState("seller", Constants.OrleansStorage)] IPersistentState<Seller> seller,
        [PersistentState("orderEntryCache", Constants.OrleansStorage)] IPersistentState<Dictionary<(int,int),List<int>>> cache,
                           AppConfig cfg,
                           IAuditLogger audit,
                           ILogger<SellerViewServiceCore> log)
    { _dbFactory=dbFactory; _seller=seller; _cache=cache; _cfg=cfg; _audit=audit; _log=log; }

    public override async Task OnActivateAsync(CancellationToken _)
    {
        
        int sid = (int)this.GetPrimaryKeyLong();
        // await using var db = _dbFactory.CreateDbContext();
        // _log.LogInformation(
        //     "ActorCtx uses {cs}",
        //     db.Database.GetDbConnection().ConnectionString);
        

        /* ① 用新的 DbContext 建&刷视图（如果已存在就跳过） */
        await using (var db = _dbFactory.CreateDbContext())
        {
            db.Database.EnsureCreated();
            
            // 建视图：加 IF NOT EXISTS 保险起见
            string createSql   = SellerDbContext.CreateSellerViewSql(sid)
                .Replace("CREATE MATERIALIZED VIEW",
                    "CREATE MATERIALIZED VIEW IF NOT EXISTS");
            await db.Database.ExecuteSqlRawAsync(createSql);
        
            // 第一次也刷新一下，让视图里至少有空结果集
            await db.Database.ExecuteSqlRawAsync(
                SellerDbContext.RefreshSellerViewSql(sid));
        }
        
        // var db     = _dbFactory.CreateDbContext();     // 每个激活期一个 DbContext
        var repo   = new OrleansSellerViewRepository(_cache, _dbFactory);
        var viewRf = new PostgresViewRefresher(_dbFactory);

        _svc = new SellerViewServiceCore(
                 sellerId : (int)this.GetPrimaryKeyLong(),
                 repo, viewRf,
                 audit : new AuditLogAdapter(_audit),
                 clock : SystemClock.Instance,
                 log   : _log,
                 logRecords : _cfg.LogRecords);
        await Task.CompletedTask;
    }

    /*—— 接口转调（与上同） ——*/
    public Task SetSeller(Seller s)                         => _svc.SetSeller(s);
    public Task<Seller?> GetSeller()                        => _svc.GetSeller();
    public Task ProcessNewInvoice(InvoiceIssued v)          => _svc.ProcessNewInvoice(v);
    // public async Task ProcessNewInvoice(InvoiceIssued v)
    // {
    //     try
    //     {
    //         await _svc.ProcessNewInvoice(v);
    //     }
    //     catch (PostgresException pgEx)
    //     {
    //         // 1) 记录日志
    //         _log.LogError(pgEx, "处理新发票时写数据库失败：{Message}", pgEx.Message);
    //
    //         // 2) 抛出 Orleans 可序列化的异常类型
    //         throw new OrleansException($"数据库写入失败：{pgEx.Message}");
    //     }
    // }
    public Task ProcessPaymentConfirmed(PaymentConfirmed v) => _svc.ProcessPaymentConfirmed(v);
    public Task ProcessPaymentFailed(PaymentFailed v)       => _svc.ProcessPaymentFailed(v);
    public Task ProcessShipmentNotification(ShipmentNotification v)
                                                        => _svc.ProcessShipmentNotification(v);
    public Task ProcessDeliveryNotification(DeliveryNotification v)
                                                        => _svc.ProcessDeliveryNotification(v);
    // public Task<SellerDashboard> QueryDashboard()           => _svc.QueryDashboard();
    public async Task<SellerDashboard> QueryDashboard()
    {
        try
        {
            // 调用你的业务逻辑
            return await _svc.QueryDashboard();
        }
        catch (PostgresException pgEx)
        {
            // 记录日志
            _log.LogError(pgEx, "QueryDashboard 从 Postgres 读取视图失败：{Message}", pgEx.Message);
            // 抛出 Orleans 友好的异常
            throw new OrleansException($"数据库错误：{pgEx.Message}");
        }
        catch (DbUpdateException dbEx)
        {
            _log.LogError(dbEx, "QueryDashboard 更新数据库失败：{Message}", dbEx.Message);
            throw new OrleansException($"数据更新错误：{dbEx.Message}");
        }
        catch (Exception ex)
        {
            // 捕获其它所有异常，防止任何“未知类型”走出 Grain
            _log.LogError(ex, "QueryDashboard 遇到未知错误");
            throw new OrleansException("内部错误，请查看服务器日志。");
        }
    }
    public Task Reset()                                     => _svc.Reset();
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        /* ① 用新的 DbContext 建&刷视图（如果已存在就跳过） */
        await using (var db = _dbFactory.CreateDbContext())
        {
            db.Database.EnsureCreated();
            
            // 建视图：加 IF NOT EXISTS 保险起见
            string createSql   = SellerDbContext.CreateCustomOrderSellerViewSql(sid)
                .Replace("CREATE MATERIALIZED VIEW",
                    "CREATE MATERIALIZED VIEW IF NOT EXISTS");
            await db.Database.ExecuteSqlRawAsync(createSql);

            // 第一次也刷新一下，让视图里至少有空结果集
            await db.Database.ExecuteSqlRawAsync(
                SellerDbContext.GetRefreshCustomOrderSellerViewSql(sid));
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
    public Task ProcessPaymentConfirmed(PaymentConfirmed v) => _svc.ProcessPaymentConfirmed(v);
    public Task ProcessPaymentFailed(PaymentFailed v)       => _svc.ProcessPaymentFailed(v);
    public Task ProcessShipmentNotification(ShipmentNotification v)
                                                        => _svc.ProcessShipmentNotification(v);
    public Task ProcessDeliveryNotification(DeliveryNotification v)
                                                        => _svc.ProcessDeliveryNotification(v);
    public Task<SellerDashboard> QueryDashboard()           => _svc.QueryDashboard();
    public Task Reset()                                     => _svc.Reset();
}

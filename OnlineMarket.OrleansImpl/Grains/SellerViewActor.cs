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
        

        /* ① Create & refresh the view with a new DbContext (skip if it already exists) */
        await using (var db = _dbFactory.CreateDbContext())
        {
            db.Database.EnsureCreated();
            
            string createSql   = SellerDbContext.CreateSellerViewSql(sid)
                .Replace("CREATE MATERIALIZED VIEW",
                    "CREATE MATERIALIZED VIEW IF NOT EXISTS");
            await db.Database.ExecuteSqlRawAsync(createSql);
        
            // Refresh the first time as well, so that there is at least an empty result set in the view
            await db.Database.ExecuteSqlRawAsync(
                SellerDbContext.RefreshSellerViewSql(sid));
        }
        
        // var db     = _dbFactory.CreateDbContext();     // One DbContext per activation
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

    /*—— Interface ——*/
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
    //         // 1) Log
// _log.LogError(pgEx, "Failed to write to database when processing new invoice: {Message}", pgEx.Message);
//
// // 2) Throw Orleans serializable exception type
// throw new OrleansException($"Failed to write to database: {pgEx.Message}");
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
            // Call logic
            return await _svc.QueryDashboard();
        }
        catch (PostgresException pgEx)
        {
            // log
            _log.LogError(pgEx, "QueryDashboard fails to read view from Postgres：{Message}", pgEx.Message);
            // Throw Orleans-friendly exceptions
            throw new OrleansException($"Database Error：{pgEx.Message}");
        }
        catch (DbUpdateException dbEx)
        {
            _log.LogError(dbEx, "QueryDashboard failed to update the database：{Message}", dbEx.Message);
            throw new OrleansException($"Data update error：{dbEx.Message}");
        }
        catch (Exception ex)
        {
            // Catch all other exceptions to prevent any "unknown type" from escaping the grain
            _log.LogError(ex, "QueryDashboard encountered an unknown error");
            throw new OrleansException("Internal error, please check the server logs.");
        }
    }
    public Task Reset()                                     => _svc.Reset();
}

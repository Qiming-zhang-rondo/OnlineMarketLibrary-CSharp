using Orleans.Concurrency;
using Orleans.Runtime;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Interfaces;
using Microsoft.Extensions.Logging;

namespace OnlineMarket.OrleansImpl.Grains;

[Reentrant]
public sealed class SellerActor : Grain, ISellerActor
{
    // [PersistentState("seller", Constants.OrleansStorage)]
    private readonly IPersistentState<Seller> _seller;
    // [PersistentState("orderEntries", Constants.OrleansStorage)]
    private readonly IPersistentState<Dictionary<string,List<OrderEntry>>> _entries;

    private readonly AppConfig _cfg;
    private readonly IAuditLogger _audit;
    private readonly ILogger<SellerServiceCore> _log;
    private SellerServiceCore _svc = null!;

    public SellerActor([PersistentState("seller", Constants.OrleansStorage)] IPersistentState<Seller> seller,
        [PersistentState("orderEntries", Constants.OrleansStorage)] IPersistentState<Dictionary<string,List<OrderEntry>>> entries,
                       AppConfig cfg,
                       IAuditLogger audit,
                       ILogger<SellerServiceCore> log)
    { _seller = seller; _entries = entries; _cfg = cfg; _audit = audit; _log = log; }

    public override Task OnActivateAsync(CancellationToken _)
    {
        var repo  = new OrleansSellerRepository(_seller, _entries);
        var clock = SystemClock.Instance;
        var audit = new AuditLogAdapter(_audit);

        _svc = new SellerServiceCore(
                   (int)this.GetPrimaryKeyLong(),
                   repo, audit, clock, _log,
                   _cfg.LogRecords);

        return Task.CompletedTask;
    }

    /*── ISellerActor → Core ─*/
    public Task SetSeller(Seller s)                            => _svc.SetSeller(s);
    public Task<Seller?> GetSeller()                           => _svc.GetSeller();
    public Task ProcessNewInvoice(InvoiceIssued v)             => _svc.ProcessNewInvoice(v);
    public Task ProcessPaymentConfirmed(PaymentConfirmed v)    => _svc.ProcessPaymentConfirmed(v);
    public Task ProcessPaymentFailed(PaymentFailed v)          => _svc.ProcessPaymentFailed(v);
    public Task ProcessShipmentNotification(ShipmentNotification v)
                                                            => _svc.ProcessShipmentNotification(v);
    public Task ProcessDeliveryNotification(DeliveryNotification v)
                                                            => _svc.ProcessDeliveryNotification(v);
    public Task<SellerDashboard> QueryDashboard()              => _svc.QueryDashboard();
    public Task Reset()                                        => _svc.Reset();
}

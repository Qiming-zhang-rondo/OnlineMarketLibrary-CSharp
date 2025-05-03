// OnlineMarket.OrleansImpl.Grains/OrderActor.cs

using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Services;          // OrderServiceCore
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans.Concurrency;
using Orleans.Runtime;

[Reentrant]
public sealed class OrderActor : Grain, IOrderActor
{
    private readonly IPersistentState<Dictionary<int,OrderGrainState>>   _orders;
    private readonly IPersistentState<OrderIdCounter>             _id;
    private readonly AppConfig   _cfg;
    private readonly IAuditLogger _audit;
    private readonly ILogger<OrderServiceCore> _log;

    private OrderServiceCore _svc = null!;

    public OrderActor(
        [PersistentState("orders",      Constants.OrleansStorage)] IPersistentState<Dictionary<int,OrderGrainState>> orders,
        [PersistentState("nextOrderId", Constants.OrleansStorage)] IPersistentState<OrderIdCounter> id,
        AppConfig cfg,
        IAuditLogger audit,
        ILogger<OrderServiceCore> log)
    { _orders = orders; _id = id; _cfg = cfg; _audit=audit; _log = log; }

    public override Task OnActivateAsync(CancellationToken _)
    {
        var repo   = new OrleansOrderRepository(_orders,_id);
        var stock  = new StockGrainReserver(GrainFactory);
        ISellerNotifier  seller = _cfg.SellerViewPostgres
                   ? new SellerViewGrainNotifier(GrainFactory)
                   : new SellerGrainNotifier(GrainFactory);
        var pay    = new PaymentGrainGateway(GrainFactory);
        var clock  = SystemClock.Instance;

        _svc = new OrderServiceCore(
                   customerId: (int)this.GetPrimaryKeyLong(),
                   repo, stock, seller, pay,
                   clock, _log);

        return Task.CompletedTask;
    }

    /*──────── IOrderActor → Core ───────*/
    public Task Checkout(ReserveStock rs)                     => _svc.Checkout(rs);
    public Task ProcessPaymentConfirmed(PaymentConfirmed v)   => _svc.ProcessPaymentConfirmed(v);
    public Task ProcessPaymentFailed(PaymentFailed v)         => _svc.ProcessPaymentFailed(v);
    public Task ProcessShipmentNotification(ShipmentNotification v)
                                                             => _svc.ProcessShipmentNotification(v);
    public Task<List<Order>> GetOrders()                      => _svc.GetOrders();
    public Task<int>        GetNumOrders()                    => _svc.GetNumOrders();
    public Task Reset()                                       => _svc.Reset();
}

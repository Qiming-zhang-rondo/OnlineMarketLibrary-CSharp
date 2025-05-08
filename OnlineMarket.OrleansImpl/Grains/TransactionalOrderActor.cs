// OnlineMarket.OrleansImpl.Grains/Transactional/TransactionalOrderActor.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Transactions.Abstractions;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Interfaces;

namespace OnlineMarket.OrleansImpl.Grains.Transactional;

[Reentrant]   // 粒度可重入，事务自己保证一致性
public sealed class TransactionalOrderActor
        : Grain, ITransactionalOrderActor
{
    /*────────────────────── Orleans Tx‑state ──────────────────────*/

    private readonly ITransactionalState<Dictionary<int, OrderGrainState>> _orders;
    private readonly ITransactionalState<OrderIdCounter>                   _id;

    /*────────────────── 其他依赖（通过 DI 注入） ──────────────────*/

    private readonly AppConfig          _cfg;
    private readonly IAuditLogger       _audit;
    private readonly ILogger<OrderServiceCore> _log;

    private OrderServiceCore _svc = null!;   // activate 时初始化

    /*────────────────────── ctor ────────────────────────────────*/
    public TransactionalOrderActor(
        [TransactionalState("orders",      Constants.OrleansStorage)]
        ITransactionalState<Dictionary<int,OrderGrainState>> orders,

        [TransactionalState("nextOrderId", Constants.OrleansStorage)]
        ITransactionalState<OrderIdCounter> id,

        AppConfig            cfg,
        IAuditLogger         audit,
        ILogger<OrderServiceCore> log)
    {
        _orders = orders;
        _id     = id;
        _cfg    = cfg;
        _audit  = audit;
        _log    = log;
    }

    /*────────────────── 生命周期：构建 Core Service ───────────────*/
    public override Task OnActivateAsync(CancellationToken _)
    {
        var repo  = new TxOrderRepository(_orders, _id);
        var stock = new StockGrainReserver(GrainFactory);

        ISellerNotifier sellerNtfy = _cfg.SellerViewPostgres
            ? new SellerViewGrainNotifier(GrainFactory)
            : new SellerGrainNotifier(GrainFactory);

        var paymentGw = new PaymentGrainGateway(GrainFactory);
        var clock     = SystemClock.Instance;

        _svc = new OrderServiceCore(
                  customerId: (int)this.GetPrimaryKeyLong(),
                  repo, stock, sellerNtfy, paymentGw,
                  clock, _log);

        return Task.CompletedTask;
    }

    /*────────────────── ITransactionalOrderActor ────────────────*/

    /*──────── 写模型：Start / Join 事务 ───────*/
    public Task Checkout(ReserveStock rs)                         => _svc.Checkout(rs);
    public Task ProcessPaymentConfirmed(PaymentConfirmed v)       => _svc.ProcessPaymentConfirmed(v);
    public Task ProcessPaymentFailed(PaymentFailed v)             => _svc.ProcessPaymentFailed(v);
    public Task ProcessShipmentNotification(ShipmentNotification v)
                                                                   => _svc.ProcessShipmentNotification(v);

    /*──────── 读查询：CreateOrJoin ───────────*/
    public Task<List<Order>> GetOrders()                          => _svc.GetOrders();
    public Task<int>        GetNumOrders()                        => _svc.GetNumOrders();

    /*──────── 测试 / 运维 ───────────*/
    public Task Reset()                                           => _svc.Reset();

    
}

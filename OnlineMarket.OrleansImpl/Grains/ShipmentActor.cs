using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;          // ← 适配器实现所在命名空间

using Orleans.Runtime;
using Orleans.Concurrency;
namespace OnlineMarket.OrleansImpl.Grains;

[Reentrant]
public sealed class ShipmentActor : Grain, IShipmentActor
{
    // [PersistentState("shipments", Constants.OrleansStorage)]
    private readonly IPersistentState<SortedDictionary<int,Shipment>> _ship;
    // [PersistentState("packages", Constants.OrleansStorage)]
    private readonly IPersistentState<SortedDictionary<int,List<Package>>> _pkg;
    // [PersistentState("nextShipmentId", Constants.OrleansStorage)]
    private readonly IPersistentState<NextShipmentIdState> _id;

    private readonly AppConfig _cfg;
    private readonly ILogger<ShipmentServiceCore> _log;
    private readonly IAuditLogger _audit;
    private ShipmentServiceCore _svc = null!;

    public ShipmentActor(
        [PersistentState("shipments", Constants.OrleansStorage)] IPersistentState<SortedDictionary<int,Shipment>> ship,
        [PersistentState("packages", Constants.OrleansStorage)] IPersistentState<SortedDictionary<int,List<Package>>> pkg,
        [PersistentState("nextShipmentId", Constants.OrleansStorage)] IPersistentState<NextShipmentIdState> id,
        AppConfig cfg,
        IAuditLogger audit,
        ILogger<ShipmentServiceCore> log)
    { _ship = ship; _pkg = pkg; _id = id; _cfg = cfg; _audit = audit; _log = log;}

    public override Task OnActivateAsync(CancellationToken _) 
    {
        var repo   = new OrleansShipmentRepository(_ship, _pkg, _id);
        var clock  = SystemClock.Instance;
        var audit = new AuditLogAdapter(_audit);

        ISellerNotifier sellerNtfy = _cfg.SellerViewPostgres
               ? new SellerViewGrainNotifier(GrainFactory)
               : new SellerGrainNotifier(GrainFactory);

        var orderNtfy  = new OrderGrainNotifier(GrainFactory);

        _svc = new ShipmentServiceCore(repo, sellerNtfy, orderNtfy, audit, clock, _log);
        return Task.CompletedTask;
    }

    /*── IShipmentActor 接口：全部委托给 _svc ──*/
    public Task ProcessShipment(PaymentConfirmed e)                => _svc.ProcessShipment(e);
    public Task UpdateShipment(string tid)                         => _svc.UpdateShipment(tid);
    public Task UpdateShipment(string tid, ISet<(int,int,int)> s)  => _svc.UpdateShipment(tid, s);
    public Task<List<Shipment>> GetShipments(int cid)              => _svc.GetShipments(cid);
    public Task Reset()                                            => _svc.Reset();
}

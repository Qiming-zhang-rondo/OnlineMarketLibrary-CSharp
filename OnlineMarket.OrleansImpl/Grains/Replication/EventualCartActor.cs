// OnlineMarket.OrleansImpl.Grains/EventualCartActor.cs
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Services.Replication;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.OrleansImpl.Interfaces.Replication;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Streams;

namespace OnlineMarket.OrleansImpl.Grains.Replication;

[Reentrant]
public sealed class EventualCartActor : Grain, IEventualCartActor
{
    private readonly IPersistentState<Cart> _state;
    private readonly AppConfig _cfg;
    private readonly ILogger<EventualCartServiceCore> _log;
    private EventualCartServiceCore _svc=null!;

    public EventualCartActor(
        [PersistentState("cart",Constants.OrleansStorage)]
        IPersistentState<Cart> state,
        AppConfig cfg,
        ILogger<EventualCartServiceCore> log)
    { _state=state; _cfg=cfg; _log=log; }

    public override Task OnActivateAsync(CancellationToken _)
    {
        var repo   = new OrleansCartRepository(_state);
        var order  = new OrderGrainGateway(GrainFactory);
        var clock  = SystemClock.Instance;

        /* ① 构造 Orleans-Stream Adapter */
        var provider = this.GetStreamProvider(Constants.DefaultStreamProvider);
        var updates  = new OrleansProductUpdateGateway(provider);

        _svc = new EventualCartServiceCore(
            (int)this.GetPrimaryKeyLong(),
            repo, order, updates, clock, _log,
            trackHistory:_cfg.TrackCartHistory);

        return Task.CompletedTask;
    }

    /*──── IEventualCartActor 全部转调 ────*/
    public Task<Cart> GetCart() => _svc.GetCartAsync();
    public Task<List<CartItem>> GetItems()
        => _svc.GetItemsAsync().ContinueWith(t=>t.Result.ToList());
    public Task AddItem(CartItem i) => _svc.AddItemAsync(i);
    public Task NotifyCheckout(CustomerCheckout c)=>_svc.NotifyCheckoutAsync(c);
    public Task Seal()=>_svc.SealAsync();
    public Task<List<CartItem>> GetHistory(string id)
        => _svc.GetHistoryAsync(id).ContinueWith(t=>t.Result.ToList());

    /* 额外调试接口：拿缓存里的最新 Product */
    public Task<Product?> GetReplicaItem(int sid,int pid)
        => Task.FromResult(_svc.TryGetReplica(sid,pid));
}

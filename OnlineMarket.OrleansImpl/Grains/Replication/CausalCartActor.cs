using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Services.Replication;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Infra.Redis;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.OrleansImpl.Interfaces.Replication;
using Orleans.Concurrency;
using Orleans.Runtime;

[Reentrant]
public sealed class CausalCartActor : Grain, ICausalCartActor
{
    private readonly IPersistentState<Cart> _state;
    private readonly AppConfig              _cfg;
    private readonly ILogger<CausalCartServiceCore> _log;
    private readonly IProductReplicaGateway  _replica;

    private CausalCartServiceCore _svc = null!;

    public CausalCartActor(
        [PersistentState("cart", Constants.OrleansStorage)]
        IPersistentState<Cart> state,
        AppConfig cfg,
        ILogger<CausalCartServiceCore> log,
        IProductReplicaGateway   replica)
    { _state = state; _cfg = cfg; _log = log; _replica = replica; }

    public override Task OnActivateAsync(CancellationToken _)
    {
        var repo    = new OrleansCartRepository(_state);
        var order   = new OrderGrainGateway(GrainFactory);
        var clock   = SystemClock.Instance;
        // var replica = new InMemoryProductReplicaGateway(_replica);

        _svc = new CausalCartServiceCore(
            (int)this.GetPrimaryKeyLong(),
            repo, order, _replica, clock, _log,
            trackHistory: _cfg.TrackCartHistory);

        return Task.CompletedTask;
    }

    /*── ICausalCartActor 接口转调 ────────*/
    public Task<Cart> GetCart()                       => _svc.GetCartAsync();
    public Task<List<CartItem>> GetItems()            => _svc.GetItemsAsync().ContinueWith(t => t.Result.ToList());
    public Task AddItem(CartItem i)                   => _svc.AddItemAsync(i);
    public Task NotifyCheckout(CustomerCheckout cc)   => _svc.NotifyCheckoutAsync(cc);
    public Task Seal()                                => _svc.SealAsync();
    public Task<List<CartItem>> GetHistory(string id) => _svc.GetHistoryAsync(id).ContinueWith(t => t.Result.ToList());
    public Task<ProductReplica?> GetReplicaItem(int sellerId, int productId) =>
        _replica.GetReplicaAsync(sellerId, productId);
}

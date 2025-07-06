// OnlineMarket.OrleansImpl.Grains/CartActor.cs

using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans.Concurrency;
using Orleans.Runtime;

[Reentrant]
public sealed class CartActor : Grain, ICartActor
{
    private readonly IPersistentState<Cart> _state;
    private readonly AppConfig   _cfg;
    private readonly ILogger<CartServiceCore> _log;
    private CartServiceCore _svc = null!;

    public CartActor(
        [PersistentState("cart", Constants.OrleansStorage)]
        IPersistentState<Cart> state,
        AppConfig cfg,
        ILogger<CartServiceCore> log)
    { _state = state; _cfg = cfg; _log = log; }

    public override Task OnActivateAsync(CancellationToken _)
    {
        var repo   = new OrleansCartRepository(_state);
        var order  = new OrderGrainGateway(GrainFactory);
        var clock  = SystemClock.Instance;

        _svc = new CartServiceCore(
            (int)this.GetPrimaryKeyLong(),
            repo, order, clock, _log,
            trackHistory: _cfg.TrackCartHistory);

        return Task.CompletedTask;
    }

    /*── ICartActor Interface ──────────*/
    public Task AddItem(CartItem i)                   => _svc.AddItemAsync(i);
    public Task<List<CartItem>> GetItems()            => _svc.GetItemsAsync().ContinueWith(t=>t.Result.ToList());
    public Task NotifyCheckout(CustomerCheckout cc)   => _svc.NotifyCheckoutAsync(cc);
    public Task<Cart> GetCart()                       => _svc.GetCartAsync();
    public Task Seal()                                => _svc.SealAsync();
    public Task<List<CartItem>> GetHistory(string id) => _svc.GetHistoryAsync(id).ContinueWith(t=>t.Result.ToList());
}
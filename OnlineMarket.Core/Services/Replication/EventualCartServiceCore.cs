namespace OnlineMarket.Core.Services.Replication;

// OnlineMarket.Core.Services/EventualCartServiceCore.cs
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;
using Microsoft.Extensions.Logging;

public sealed class EventualCartServiceCore : ICartService
{
    private readonly int                    _customerId;
    private readonly ICartRepository        _repo;
    private readonly IOrderGateway          _order;
    private readonly IProductUpdateGateway  _updates;
    private readonly IClock                 _clock;
    private readonly ILogger                _log;
    private readonly bool                   _track;

    private Cart _cart;
    private readonly Dictionary<string,List<CartItem>> _hist = new();
    private readonly Dictionary<(int,int),Product>     _cache = new();

    public EventualCartServiceCore(
        int customerId,
        ICartRepository repo,
        IOrderGateway order,
        IProductUpdateGateway updates,
        IClock clock,
        ILogger log,
        bool trackHistory=false)
    {
        _customerId = customerId;
        _repo       = repo;
        _order      = order;
        _updates    = updates;
        _clock      = clock;
        _log        = log;
        _track      = trackHistory;

        _cart = repo.LoadAsync(customerId).GetAwaiter().GetResult()
              ?? new Cart(customerId);
    }

    /*──────── 订阅回调 ────────*/
    private Task OnProductChanged(Product p)
    {
        _cache[(p.seller_id,p.product_id)] = p;
        return Task.CompletedTask;
    }

    /*──────── ICartService ────────*/
    public Task<Cart> GetCartAsync() => Task.FromResult(_cart);
    public Task<IReadOnlyList<CartItem>> GetItemsAsync()
        => Task.FromResult<IReadOnlyList<CartItem>>(_cart.items);

    public async Task AddItemAsync(CartItem item)
    {
        if(item.Quantity<=0)  throw new InvalidOperationException("qty<=0");
        if(_cart.status==CartStatus.CHECKOUT_SENT)
            throw new InvalidOperationException("cart in checkout");

        /* ① 订阅该商品更新 */
        await _updates.SubscribeAsync(item.SellerId,item.ProductId,OnProductChanged);

        /* ② 入篮并持久化 */
        _cart.items.Add(item);
        await _repo.SaveAsync(_cart);
    }

    public async Task NotifyCheckoutAsync(CustomerCheckout cc)
    {
        /* ① 用缓存刷新价格 */
        foreach(var it in _cart.items)
        {
            if(!_cache.TryGetValue((it.SellerId,it.ProductId),out var p)) continue;
            if(it.Version.SequenceEqual(p.version) && it.UnitPrice > p.price)
            {
                var diff = it.UnitPrice - p.price;
                it.Voucher += diff;
                it.UnitPrice = p.price;
            }
        }

        /* ② 正常结账流程 */
        _cart.status = CartStatus.CHECKOUT_SENT;
        if(_track) _hist.TryAdd(cc.instanceId,new(_cart.items));
        var rs = new ReserveStock(_clock.UtcNow,cc,_cart.items,cc.instanceId);
        await _order.CheckoutAsync(rs);
        await SealAsync();

        /* ③ 结束后取消订阅 */
        await _updates.UnsubscribeAllAsync();
        _cache.Clear();
    }

    public async Task SealAsync()
    {
        _cart.status = CartStatus.OPEN;
        _cart.items.Clear();
        await _repo.SaveAsync(_cart);
    }

    public Task<IReadOnlyList<CartItem>> GetHistoryAsync(string tid)=>
        Task.FromResult<IReadOnlyList<CartItem>>(
            _hist.TryGetValue(tid,out var l)? l : new());
    
    public Product? TryGetReplica(int sellerId, int productId)
    {
        _cache.TryGetValue((sellerId, productId), out var p);
        return p;
    }

    public async Task ResetAsync()
    {
        _cart = new Cart(_customerId);
        await _updates.UnsubscribeAllAsync();
        _cache.Clear();
        await _repo.ClearAsync(_customerId);
    }
}

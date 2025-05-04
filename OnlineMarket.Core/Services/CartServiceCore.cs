// OnlineMarket.Core.Services/CartServiceCore.cs

using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;

public sealed class CartServiceCore : ICartService
{
    private readonly int            _customerId;
    private readonly ICartRepository _repo;
    private readonly IOrderGateway   _order;
    private readonly bool            _trackHistory;
    private readonly IClock          _clock;
    private readonly ILogger         _log;

    private Cart _cart;                                     // 当前快照
    private readonly Dictionary<string,List<CartItem>> _hist = new();

    public CartServiceCore(int customerId,
                           ICartRepository repo,
                           IOrderGateway   order,
                           IClock clock,
                           ILogger log,
                           bool  trackHistory = false)
    {
        _customerId   = customerId;
        _repo         = repo;
        _order        = order;
        _clock        = clock;
        _log          = log;
        _trackHistory = trackHistory;

        // 初始化快照（若仓库中没有则新建）
        _cart = repo.LoadAsync(customerId).GetAwaiter().GetResult()
              ?? new Cart(customerId);
    }

    /*──────── ICartService 实现 ────────*/

    public Task<Cart> GetCartAsync()   => Task.FromResult(_cart);
    public Task<IReadOnlyList<CartItem>> GetItemsAsync() => 
        Task.FromResult<IReadOnlyList<CartItem>>(_cart.items);

    public async Task AddItemAsync(CartItem item)
    {
        if (item.Quantity <= 0)
            throw new InvalidOperationException("Quantity must be > 0");

        if (_cart.status == CartStatus.CHECKOUT_SENT)
            throw new InvalidOperationException("Cart already in checkout");

        _cart.items.Add(item);
        await _repo.SaveAsync(_cart);
    }

    public async Task NotifyCheckoutAsync(CustomerCheckout cc)
    {
        _cart.status = CartStatus.CHECKOUT_SENT;
        var rs = new ReserveStock(_clock.UtcNow, cc, _cart.items, cc.instanceId);

        if (_trackHistory)
            _hist.TryAdd(cc.instanceId, new(_cart.items));

        await _order.CheckoutAsync(rs);        // 调用网关
        await SealAsync();
    }

    public async Task SealAsync()
    {
        _cart.status = CartStatus.OPEN;
        _cart.items.Clear();
        await _repo.SaveAsync(_cart);
    }

    public Task<IReadOnlyList<CartItem>> GetHistoryAsync(string tid) =>
        Task.FromResult<IReadOnlyList<CartItem>>(
            _hist.TryGetValue(tid, out var l) ? l : new());

    public async Task ResetAsync()
    {
        _cart = new Cart(_customerId);
        await _repo.ClearAsync(_customerId);
    }
}

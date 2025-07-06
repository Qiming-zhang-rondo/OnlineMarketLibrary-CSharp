namespace OnlineMarket.Core.Services.Replication;

using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;

public sealed class CausalCartServiceCore : ICartService
{
    private readonly int                   _customerId;
    private readonly ICartRepository       _repo;
    private readonly IOrderGateway         _order;
    private readonly IProductReplicaGateway _replica;
    private readonly IClock                _clock;
    private readonly ILogger               _log;
    private readonly bool                  _trackHistory;

    private Cart _cart;
    private readonly Dictionary<string, List<CartItem>> _hist = new();

    public CausalCartServiceCore(
        int customerId,
        ICartRepository repo,
        IOrderGateway order,
        IProductReplicaGateway replica,
        IClock clock,
        ILogger log,
        bool trackHistory = true)
    {
        _customerId   = customerId;
        _repo         = repo;
        _order        = order;
        _replica      = replica;
        _clock        = clock;
        _log          = log;
        _trackHistory = trackHistory;

        _cart = repo.LoadAsync(customerId).GetAwaiter().GetResult()
              ?? new Cart(customerId);
    }

    /*──────── ICartService ────────*/

    public Task<Cart> GetCartAsync() =>
        Task.FromResult(_cart);

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
        /* ① Perform causal consistency check on each line of goods before checkout */
        foreach (var it in _cart.items)
        {
            var replica = await _replica.GetReplicaAsync(it.SellerId, it.ProductId);
            if (replica is null)
            {
                _log.LogWarning("Replica not found for {Sid}-{Pid}", it.SellerId, it.ProductId);
                continue;                      // 商品已下架 → 允许旧价结算
            }

            if (it.Version.SequenceEqual(replica.Version))
            {
                if (it.UnitPrice > replica.Price)
                {
                    var diff =  it.UnitPrice - replica.Price;
                    it.Voucher += diff;       
                    it.UnitPrice = replica.Price;
                }
            }
            // Version inconsistency: old price is allowed (product is deleted or changed)
        }

        /* ② Enter the checkout process normally */
        _cart.status = CartStatus.CHECKOUT_SENT;
        if (_trackHistory)
            _hist.TryAdd(cc.instanceId, new(_cart.items));

        var rs = new ReserveStock(_clock.UtcNow, cc, _cart.items, cc.instanceId);
        await _order.CheckoutAsync(rs);
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

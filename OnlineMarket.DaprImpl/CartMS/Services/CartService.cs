// DaprImpl/CartMS/Services/CartService.cs

using Dapr.Client;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Driver;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Common.Requests;

namespace CartMS.Services;

/// <summary>
/// Dapr 实现层的 CartService：
/// - 封装 core.CartServiceCore
/// - 提供事件订阅处理方法
/// </summary>
public sealed class CartService : ICartService
{
    private readonly ICartService _coreService;
    private readonly DaprClient _dapr;
    private readonly ILogger<CartService> _log;

    private const string PUBSUB_NAME = "pubsub";

    public CartService(
        ICartService coreService,
        DaprClient dapr,
        ILogger<CartService> log)
    {
        _coreService = coreService;
        _dapr = dapr;
        _log = log;
    }

    /*──────── 转发 ICartService ────────*/

    public Task AddItemAsync(OnlineMarket.Core.Common.Entities.CartItem item)
        => _coreService.AddItemAsync(item);

    public Task<OnlineMarket.Core.Common.Entities.Cart> GetCartAsync()
        => _coreService.GetCartAsync();

    public Task<IReadOnlyList<OnlineMarket.Core.Common.Entities.CartItem>> GetHistoryAsync(string tid)
        => _coreService.GetHistoryAsync(tid);

    public Task<IReadOnlyList<OnlineMarket.Core.Common.Entities.CartItem>> GetItemsAsync()
        => _coreService.GetItemsAsync();

    public Task NotifyCheckoutAsync(OnlineMarket.Core.Common.Requests.CustomerCheckout checkout)
        => _coreService.NotifyCheckoutAsync(checkout);

    public Task ResetAsync()
        => _coreService.ResetAsync();

    public Task SealAsync()
        => _coreService.SealAsync();


    /*──────── Dapr 专用订阅处理逻辑 ────────*/

    public async Task HandleProductUpdatedAsync(ProductUpdated evt)
    {
        _log.LogInformation("Processing ProductUpdated event: {0}", evt.productId);

        // 这里你可以实现自己的业务逻辑，例如更新缓存
        // ➔ 因为 core 里不提供直接更新接口，我们这里只演示 log
    }

    public async Task HandlePriceUpdatedAsync(PriceUpdate evt)
    {
        _log.LogInformation("Processing PriceUpdated event: {0}", evt.productId);

        if (int.TryParse(evt.instanceId, out var tid))
        {
            await _dapr.PublishEventAsync(
                PUBSUB_NAME,
                "PriceUpdated_Ack",
                new TransactionMark(
                    tid,
                    TransactionType.PRICE_UPDATE,
                    evt.sellerId,
                    MarkStatus.SUCCESS,
                    "cart"
                )
            );
        }
        else
        {
            _log.LogError("Invalid instanceId (cannot parse to int): {0}", evt.instanceId);
            
        }
    }

    public async Task HandlePoisonProductUpdatedAsync(ProductUpdated evt)
    {
        _log.LogWarning("Poison ProductUpdated event: {0}", evt.productId);
        await _dapr.PublishEventAsync(
            PUBSUB_NAME,
            "ProductUpdated_Poison",
            evt
        );
    }

    public async Task HandlePoisonPriceUpdatedAsync(PriceUpdate evt)
    {
        _log.LogWarning("Poison PriceUpdated event: {0}", evt.productId);
        await _dapr.PublishEventAsync(
            PUBSUB_NAME,
            "PriceUpdated_Poison",
            evt
        );
    }
}
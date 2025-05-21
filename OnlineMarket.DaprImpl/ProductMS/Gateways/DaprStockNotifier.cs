using Dapr.Client;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Ports;

namespace ProductMS.Gateways;

/// <summary>
/// 通过 Dapr Pub/Sub 通知库存系统有商品更新。
/// </summary>
internal sealed class DaprStockNotifier : IStockNotifier
{
    private const string PUBSUB_NAME = "pubsub";
    private readonly DaprClient _dapr;
    private readonly ILogger<DaprStockNotifier> _log;

    public DaprStockNotifier(DaprClient dapr, ILogger<DaprStockNotifier> log)
    {
        _dapr = dapr;
        _log = log;
    }

    public async Task NotifyProductUpdated(ProductUpdated evt)
    {
        _log.LogInformation(
            "Publishing ProductUpdated: SellerId={SellerId}, ProductId={ProductId}, Version={Version}",
            evt.sellerId, evt.productId, evt.instanceId
        );

        await _dapr.PublishEventAsync(
            PUBSUB_NAME,
            nameof(ProductUpdated),
            evt
        );
    }
}
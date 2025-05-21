using Dapr.Client;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;

namespace ProductMS.Gateways;

/// <summary>
/// 实现 IReplicationPublisher：
/// - 将最新商品状态推送到消息流（Kafka / Dapr PubSub）
/// - 保存快照（这里暂时只做演示）
/// </summary>
internal sealed class DaprReplicationPublisher : IReplicationPublisher
{
    private const string PUBSUB_NAME = "pubsub";
    private readonly DaprClient _dapr;
    private readonly ILogger<DaprReplicationPublisher> _log;

    public DaprReplicationPublisher(DaprClient daprClient, ILogger<DaprReplicationPublisher> log)
    {
        _dapr = daprClient;
        _log = log;
    }

    public async Task PublishAsync(Product product)
    {
        _log.LogInformation("Publishing Product: {SellerId}-{ProductId}, Version: {Version}",
            product.seller_id, product.product_id, product.version);

        await _dapr.PublishEventAsync(
            PUBSUB_NAME,
            nameof(Product),
            product
        );
    }

    public Task SaveSnapshotAsync(Product product)
    {
        // 👇 这里为了演示，实际你可以实现 Redis 缓存 / Memory 缓存
        _log.LogInformation("Saving snapshot for Product: {SellerId}-{ProductId}, Version: {Version}",
            product.seller_id, product.product_id, product.version);

        // 暂时什么都不做
        return Task.CompletedTask;
    }
}
using Dapr.Client;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Ports;
using System.Threading.Tasks;          // 确保编译器引用的是 BCL 的 Task

namespace CartMS.Gateways;

/// <summary>
/// 通过 Dapr Pub/Sub 把 <see cref="ReserveStock"/> 发送到 order 微服务
/// </summary>
internal sealed class DaprOrderGateway : IOrderGateway
{
    private const string PUBSUB_NAME = "pubsub";
    private readonly DaprClient _dapr;

    public DaprOrderGateway(DaprClient daprClient) => _dapr = daprClient;

    // **完整匹配接口签名** —— 返回 Task
    global::System.Threading.Tasks.Task IOrderGateway.CheckoutAsync(ReserveStock rs)
        => _dapr.PublishEventAsync(PUBSUB_NAME, nameof(ReserveStock), rs);
}
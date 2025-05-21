using Dapr.Client;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Driver;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;

namespace ProductMS.Services;

/// <summary>
/// DaprProductService 实现 IProductService，
/// 内部封装 core，并在事件完成后发送 TransactionMark 确认。
/// </summary>
public sealed class DaprProductService : IProductService
{
    private const string PUBSUB_NAME = "pubsub";

    private readonly DaprClient _dapr;
    private readonly IProductService _core;
    private readonly ILogger<DaprProductService> _log;

    public DaprProductService(DaprClient daprClient, IProductService core, ILogger<DaprProductService> log)
    {
        _dapr = daprClient;
        _core = core;
        _log  = log;
    }

    public async Task SetProduct(Product product)
        => await _core.SetProduct(product);

    public async Task ProcessProductUpdate(Product product)
        => await _core.ProcessProductUpdate(product);

    public async Task<Product> GetProduct()
        => await _core.GetProduct();

    public async Task ProcessPriceUpdate(PriceUpdate evt)
         => await _core.ProcessPriceUpdate(evt);

    public async Task Reset() => await _core.Reset();

    // 👇 增加 poison 方法
    public async Task ProcessPoisonPriceUpdate(PriceUpdate evt)
    {
        if (int.TryParse(evt.instanceId, out var tid))
        {
            await _dapr.PublishEventAsync(
                PUBSUB_NAME,
                "PriceUpdated_Ack",
                new TransactionMark(
                    tid,
                    TransactionType.PRICE_UPDATE,
                    evt.sellerId,
                    MarkStatus.ERROR,
                    "product"
                )
            );

            _log.LogWarning("Published TransactionMark ERROR for InstanceId: {InstanceId}", evt.instanceId);
        }
        else
        {
            _log.LogError("Invalid instanceId (cannot parse to int) for poison: {InstanceId}", evt.instanceId);
        }
    }
}
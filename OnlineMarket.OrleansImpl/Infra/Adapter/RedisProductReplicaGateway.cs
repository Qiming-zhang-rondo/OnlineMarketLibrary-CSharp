using OnlineMarket.Core.Common.Integration;
using OnlineMarket.OrleansImpl.Infra.Redis;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;
// using Orleans.Infra.Redis;

public sealed class RedisProductReplicaGateway : IProductReplicaGateway
{
    private readonly IRedisConnectionFactory _factory;
    public RedisProductReplicaGateway(IRedisConnectionFactory factory) =>
        _factory = factory;

    public Task<ProductReplica?> GetReplicaAsync(int sellerId, int productId)
    {
        string key = $"{sellerId}-{productId}";
        return _factory.GetProductAsync(key);
    }
}

using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Interfaces.Replication;

namespace OnlineMarket.OrleansImpl.Infra.Redis;

public class RedisReplicaReader : IReplicaQueryService
{
    private readonly IRedisConnectionFactory redisFactory;

    public RedisReplicaReader(IRedisConnectionFactory redisFactory)
    {
        this.redisFactory = redisFactory;
    }

    public Task<ProductReplica> GetReplicaItem(int sellerId, int productId)
    {
        string key = $"{sellerId}-{productId}";
        return redisFactory.GetProductAsync(key);
    }
}
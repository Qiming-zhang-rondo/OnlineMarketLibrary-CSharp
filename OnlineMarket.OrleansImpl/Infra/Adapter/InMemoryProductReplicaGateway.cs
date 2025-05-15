using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

// OnlineMarket.Test.Fakes/InMemoryProductReplicaGateway.cs
using System.Collections.Concurrent;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;

public sealed class InMemoryProductReplicaGateway : IProductReplicaGateway
{
    //??? 两个实例共享同一内存 若不使用static 可以考虑手动构造一个共享实例，再注入到两个容器 在fixture
    private static readonly ConcurrentDictionary<string, ProductReplica> _cache = new();

    public Task<ProductReplica?> GetReplicaAsync(int sellerId, int productId)
    {
        var key = $"{sellerId}-{productId}";
        _cache.TryGetValue(key, out var pr);
        return Task.FromResult(pr);
    }

    /* 方便测试：对外暴露写入接口 */
    public void Seed(ProductReplica replica)
    {
        var key =replica.Key;
        _cache[key] = replica;
    }
}

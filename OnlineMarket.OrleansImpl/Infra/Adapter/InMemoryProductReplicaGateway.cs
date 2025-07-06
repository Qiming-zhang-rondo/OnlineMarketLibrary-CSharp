using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

// OnlineMarket.Test.Fakes/InMemoryProductReplicaGateway.cs
using System.Collections.Concurrent;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;

public sealed class InMemoryProductReplicaGateway : IProductReplicaGateway
{
    //The two instances share the same memory.
    //If you do not use static, you can consider manually constructing a shared instance
    //and injecting it into the two containers in the fixture.
    private static readonly ConcurrentDictionary<string, ProductReplica> _cache = new();

    public Task<ProductReplica?> GetReplicaAsync(int sellerId, int productId)
    {
        var key = $"{sellerId}-{productId}";
        _cache.TryGetValue(key, out var pr);
        return Task.FromResult(pr);
    }

    /* Convenient testing: expose the write interface externally */
    public void Seed(ProductReplica replica)
    {
        var key =replica.Key;
        _cache[key] = replica;
    }
}

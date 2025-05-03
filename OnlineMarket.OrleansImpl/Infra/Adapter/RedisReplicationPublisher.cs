// // Adapter/RedisReplicationPublisher.cs
// using System.Threading.Tasks;
// using OnlineMarket.Core.Common.Entities;
// using OnlineMarket.Core.Ports;
// using Orleans.Infra.Redis;
//
// namespace OnlineMarket.OrleansImpl.Infra.Adapter
// {
//     public sealed class RedisReplicationPublisher : IReplicationPublisher
//     {
//         private readonly IRedisConnectionFactory _redis;
//         public RedisReplicationPublisher(IRedisConnectionFactory redis) => _redis = redis;
//
//         public Task PublishAsync(Product _) => Task.CompletedTask;
//
//         public Task SaveSnapshotAsync(Product p)
//         {
//             string key = $"{p.seller_id}-{p.product_id}";
//             var replica = new ProductReplica(key, p.version, p.price);
//             return _redis.SaveProductAsync(key, replica);
//         }
//     }
// }
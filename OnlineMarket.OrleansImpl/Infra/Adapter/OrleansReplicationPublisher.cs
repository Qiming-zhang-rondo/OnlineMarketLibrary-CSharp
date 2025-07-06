// namespace OnlineMarket.OrleansImpl.Infra.Adapter;
//
// using Orleans.Streams;
// using OnlineMarket.Core.Common.Entities;
// using OnlineMarket.Core.Ports;
//
// public sealed class OrleansReplicationPublisher : IReplicationPublisher
// {
//     private readonly IStreamProvider _provider;
//     private readonly IRedisProductSnapshotStore _snapshotStore; 
//
//     public OrleansReplicationPublisher(IStreamProvider provider, IRedisProductSnapshotStore snapshotStore)
//     {
//         _provider = provider;
//         _snapshotStore = snapshotStore;
//     }
//
//     public async Task PublishAsync(Product product)
//     {
//         var stream = _provider.GetStream<Product>(
//             Constants.ProductNameSpace,
//             $"{product.seller_id}|{product.product_id}");
//
//         await stream.OnNextAsync(product);
//     }
//     
//     public Task SaveSnapshotAsync(Product product)
//     {
//         // 直接调用 Redis 缓存接口保存副本数据
//         return _snapshotStore.SaveAsync(product);
//     }
// }

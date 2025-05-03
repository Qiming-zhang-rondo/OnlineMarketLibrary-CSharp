// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Logging;
// using OnlineMarket.Core.Common.Entities;
// using OnlineMarket.Core.Common.Requests;
// using OnlineMarket.Core.Interfaces;
// using OnlineMarket.Core.Interfaces.Replication;
// using OnlineMarket.Core.Services.Replication;
// using OnlineMarket.OrleansImpl.Interfaces.Replication;
// using OnlineMarket.OrleansImpl.Infra.Redis;
// using OnlineMarket.OrleansImpl.Infra;
// using OnlineMarket.Core.Common.Config;
// using Orleans;
// using Orleans.Runtime;
// using OnlineMarket.Core.Common.Integration;
// using OnlineMarket.Core.Services;
// using OnlineMarket.OrleansImpl.Infra.Adapter;
//
// namespace OnlineMarket.OrleansImpl.Grains.Replication
// {
//     public sealed class CausalCartActor : CartActor, ICausalCartActor
//     {
//         private IRedisConnectionFactory redisFactory = null!;
//
//         public CausalCartActor(
//             [PersistentState("cart", Constants.OrleansStorage)] IPersistentState<Cart> cartState,
//             AppConfig config,
//             ILogger<CartServiceCore> logger,
//             IRedisConnectionFactory redisFactory
//             ) : base(cartState, config, logger)
//         {
//             this.redisFactory = redisFactory;
//         }
//
//         public override Task OnActivateAsync(CancellationToken cancellationToken)
//         {
//             customerId = (int)this.GetPrimaryKeyLong();
//
//             if (cartState.State is null || cartState.State.customerId == 0)
//             {
//                 cartState.State = new Cart(customerId);
//             }
//
//             cartService = new CausalCartServiceCore(
//                 customerId,
//                 logger,
//                 new OrderActorAdapter(customerId,GrainFactory),
//                 async () => await cartState.WriteStateAsync(),
//                 new RedisReplicaReader(redisFactory),
//                 trackHistory: false
//             );
//
//             return Task.CompletedTask;
//         }
//
//
//         public Task<ProductReplica> GetReplicaItem(int sellerId, int productId)
//         {
//             string key = $"{sellerId}-{productId}";
//             return redisFactory.GetProductAsync(key);
//         }
//     }
// }
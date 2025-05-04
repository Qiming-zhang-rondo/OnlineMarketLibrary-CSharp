// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Logging;
// using OnlineMarket.Core.Common.Entities;
// using OnlineMarket.Core.Common.Integration;
// using OnlineMarket.Core.Common.Requests;
// using OnlineMarket.Core.Interfaces;
// using OnlineMarket.Core.Interfaces.Replication;
//
//
// namespace OnlineMarket.Core.Services.Replication
// {
//     public class CausalCartServiceCore : CartServiceCore, ICausalCartService
//     {
//         private readonly IReplicaQueryService replicaQueryService;
//
//         public CausalCartServiceCore(
//             int customerId,
//             ILogger<CartServiceCore> logger,
//             IOrderService orderService,
//             Func<Task> saveCallback,
//             IReplicaQueryService replicaQueryService,
//             bool trackHistory = false
//         ) : base(customerId, logger, orderService, saveCallback, trackHistory)
//         {
//             this.replicaQueryService = replicaQueryService;
//         }
//
//         public override async Task NotifyCheckout(CustomerCheckout customerCheckout)
//         {
//             foreach (var item in cart.items)
//             {
//                 var productReplica = await GetReplicaItem(item.SellerId, item.ProductId);
//                 if (productReplica == null)
//                 {
//                     logger.LogWarning($"Item {item.SellerId} - {item.ProductId} not found in Redis replica.");
//                     continue;
//                 }
//
//                 if (item.Version.SequenceEqual(productReplica.Version))
//                 {
//                     if (item.UnitPrice < productReplica.Price)
//                     {
//                         item.Voucher += productReplica.Price - item.UnitPrice;
//                         item.UnitPrice = productReplica.Price;
//                     }
//                 }
//             }
//
//             await base.NotifyCheckout(customerCheckout);
//         }
//
//         public Task<ProductReplica> GetReplicaItem(int sellerId, int productId)
//         {
//             return replicaQueryService.GetReplicaItem(sellerId, productId);
//         }
//     }
// }
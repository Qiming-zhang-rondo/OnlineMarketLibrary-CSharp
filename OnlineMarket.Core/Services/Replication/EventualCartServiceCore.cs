// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Logging;
// using OnlineMarket.Core.Common.Entities;
// using OnlineMarket.Core.Common.Requests;
// using OnlineMarket.Core.Interfaces;
// using OnlineMarket.Core.Interfaces.Replication;
//
//
// namespace OnlineMarket.Core.Services.Replication
// {
//     public class EventualCartServiceCore : CartServiceCore, IEventualCartService
//     {
//         private readonly Dictionary<(int SellerId, int ProductId), Product> cachedProducts;
//
//         public EventualCartServiceCore(
//             int customerId,
//             ILogger<CartServiceCore> logger,
//             IOrderService orderService,
//             Func<Task> saveCallback,
//             Dictionary<(int SellerId, int ProductId), Product> cachedProducts,
//             bool trackHistory = false
//         ) : base(customerId, logger, orderService, saveCallback, trackHistory)
//         {
//             this.cachedProducts = cachedProducts;
//         }
//
//         public override async Task NotifyCheckout(CustomerCheckout customerCheckout)
//         {
//             foreach (var item in cart.items)
//             {
//                 var key = (item.SellerId, item.ProductId);
//                 if (!cachedProducts.TryGetValue(key, out var product))
//                 {
//                     logger.LogWarning($"Cached product not found: Seller={item.SellerId}, Product={item.ProductId}");
//                     continue;
//                 }
//
//                 if (item.Version.SequenceEqual(product.version))
//                 {
//                     if (item.UnitPrice < product.price)
//                     {
//                         item.Voucher += product.price - item.UnitPrice;
//                         item.UnitPrice = product.price;
//                     }
//                 }
//             }
//
//             await base.NotifyCheckout(customerCheckout);
//         }
//
//         public Task<Product> GetReplicaItem(int sellerId, int productId)
//         {
//             return Task.FromResult(this.cachedProducts[(sellerId, productId)]);
//         }
//     }
// }
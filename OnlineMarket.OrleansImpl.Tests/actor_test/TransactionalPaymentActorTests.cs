// // Tests/Payment/TransactionalPaymentActorTests.cs
// using OnlineMarket.Core.Common.Entities;
// using OnlineMarket.Core.Common.Events;
// using OnlineMarket.Core.Common.Requests;
// using OnlineMarket.OrleansImpl.Interfaces;
// using OnlineMarket.OrleansImpl.Tests.Infra.Transactional;
// using Orleans.TestingHost;
// using Xunit;
//
// namespace OnlineMarket.OrleansImpl.Tests.Payment;
//
// [Collection(TransactionalClusterCollection.Name)]
// public class TransactionalPaymentActorTests
// {
//     private readonly TestCluster _cluster;
//     public TransactionalPaymentActorTests(TransactionalClusterFixture fx)
//         => _cluster = fx.Cluster;
//
//     /*─── 帮助构造 ───*/
//     private static InvoiceIssued BuildInvoice(int cid, int oid, int sid)
//     {
//         var item = new OrderItem
//         {
//             order_id      = oid,
//             seller_id     = sid,
//             product_id    = 501,
//             product_name  = "demo",
//             unit_price    = 10,
//             quantity      = 1,
//             total_items   = 10,
//             total_amount  = 10,
//             freight_value = 2,
//             voucher       = 0
//         };
//
//         return new InvoiceIssued(
//             new CustomerCheckout { CustomerId = cid },
//             oid, $"{cid}-{oid}", DateTime.UtcNow, 12,
//             new() { item }, Guid.NewGuid().ToString());
//     }
//
//     /*──────────── 实际测试 ────────────*/
//     [Fact]
//     public async Task ProcessPayment_Should_Reserve_Stock()
//     {
//         /* 参数设定 */
//         int cid = 1, sid = 9, oid = 1001, pid = 501;
//
//         /* ① 先准备库存 – TransactionalStockActor.SetItem */
//         var stock = _cluster.GrainFactory
//             .GetGrain<ITransactionalStockActor>(sid, pid.ToString());
//
//         await stock.SetItem(new StockItem
//         {
//             seller_id     = sid,
//             product_id    = pid,
//             qty_available = 100,
//             qty_reserved  = 0,
//             version       = "v1",
//             created_at    = DateTime.UtcNow
//         });
//
//         /* ② 调用 TransactionalPaymentActor */
//         var pay  = _cluster.GrainFactory.GetGrain<ITransactionalPaymentActor>(cid);
//         await pay.ProcessPayment(BuildInvoice(cid, oid, sid));
//
//         /* ③ 验证库存已预留 1 */
//         var snap = await stock.GetItem();
//         Assert.Equal(1,  snap.qty_reserved);
//         Assert.Equal(99, snap.qty_available);
//     }
// }

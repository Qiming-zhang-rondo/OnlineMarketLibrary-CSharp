using Microsoft.Extensions.DependencyInjection;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.OrleansImpl.Interfaces.Replication;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
using Orleans.TestingHost;
using Test.Infra;

namespace OnlineMarket.OrleansImpl.Tests.actor_test;


    [Collection(NonTransactionalClusterCollection.Name)]
    public sealed class CausalCartActorTest : BaseTest
    {
        public CausalCartActorTest(NonTransactionalClusterFixture f) : base(f.Cluster) { }
        
        /*──── 帮助构造器 ────*/
        private static CartItem Item(int sid,int pid,string ver="1") => new()
        {
            SellerId     = sid,
            ProductId    = pid,
            ProductName  = "Demo",
            UnitPrice    = 100,
            FreightValue = 2,
            Quantity     = 1,
            Voucher      = 0,
            Version      = ver
        };

        private static StockItem Stock(int sid,int pid,string ver="1") => new()
        {
            seller_id     = sid,
            product_id    = pid,
            qty_available = 100,
            qty_reserved  = 0,
            version       = ver,
            created_at    = DateTime.UtcNow
        };

        private static CustomerCheckout Cust(int cid,string tid) => new()
        {
            CustomerId  = cid,
            instanceId  = tid,               // Cart 会把它当作历史 key
            PaymentType = PaymentType.BOLETO.ToString()
        };

        [Fact]
        public async Task CausalCart_Checkout_Should_Refresh_Price_From_Replica()
        {
            /* 参数 */
            int cid = 4001, sid = 91, pid = 501;
            string ver = "1";

            /* ① 取出同一个 In-Memory 副本网关，写入最新价格 120 */
            var replica = (InMemoryProductReplicaGateway)
                _cluster.ServiceProvider.GetRequiredService<IProductReplicaGateway>();

            replica.Seed(new ProductReplica {
                Key = $"{sid}-{pid}",
                Price     = 20,
                Version   = ver
            });

            /* ② 其余逻辑保持你原来的套路 ----------------------- */
            var stock = _cluster.GrainFactory.GetGrain<IStockActor>(sid, pid.ToString());
            await stock.SetItem( Stock(sid, pid, ver) );

            var cart  = _cluster.GrainFactory.GetGrain<ICausalCartActor>(cid);
            await cart.AddItem( Item(sid, pid, ver) );

            string tid = Guid.NewGuid().ToString();
            await cart.NotifyCheckout( Cust(cid, tid) );

            await Task.Delay(500);                        // 等异步链收尾

            /* ③ 断言 —— 价格应被刷新为 10，折扣 110 */
            var hist = await cart.GetHistory(tid);
            var line = Assert.Single(hist);
            Assert.Equal(20, line.UnitPrice);
            Assert.Equal(80,  line.Voucher);
            
        }
    }


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
            instanceId  = tid,               
            PaymentType = PaymentType.BOLETO.ToString()
        };

        [Fact]
        public async Task CausalCart_Checkout_Should_Refresh_Price_From_Replica()
        {
           
            int cid = 4001, sid = 91, pid = 501;
            string ver = "1";

            /* ① Take out the same In-Memory replica gateway and write the latest price 120 */
            var replica = (InMemoryProductReplicaGateway)
                _cluster.ServiceProvider.GetRequiredService<IProductReplicaGateway>();

            replica.Seed(new ProductReplica {
                Key = $"{sid}-{pid}",
                Price     = 20,
                Version   = ver
            });

            /* ② Keep the rest of the logic as you originally intended ----------------------- */
            var stock = _cluster.GrainFactory.GetGrain<IStockActor>(sid, pid.ToString());
            await stock.SetItem( Stock(sid, pid, ver) );

            var cart  = _cluster.GrainFactory.GetGrain<ICausalCartActor>(cid);
            await cart.AddItem( Item(sid, pid, ver) );

            string tid = Guid.NewGuid().ToString();
            await cart.NotifyCheckout( Cust(cid, tid) );

            await Task.Delay(500);                        

            /* ③ Assert —— The price should be refreshed to 20, with a discount of 80 */
            var hist = await cart.GetHistory(tid);
            var line = Assert.Single(hist);
            Assert.Equal(20, line.UnitPrice);
            Assert.Equal(80,  line.Voucher);
            
        }
    }


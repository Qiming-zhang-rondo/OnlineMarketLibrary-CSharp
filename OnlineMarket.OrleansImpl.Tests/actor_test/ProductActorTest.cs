// ──────────────────────────────────────────────────────────────
// OnlineMarket.OrleansImpl.Tests/actor_test/ProductActorTest.cs
// ──────────────────────────────────────────────────────────────
using Xunit;
using Test.Infra;                                      // 你的 BaseTest / Fixture 所在命名空间
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.OrleansImpl.Interfaces;             // IProductActor
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;

namespace OnlineMarket.OrleansImpl.Tests.actor_test
{
    [Collection(NonTransactionalClusterCollection.Name)]
    public class ProductActorTest : BaseTest
    {
        public ProductActorTest(NonTransactionalClusterFixture f) : base(f.Cluster) { }
        
        private static Product BuildProduct(int sid, int pid, float price = 10, string ver = "v1") =>
            new()
            {
                seller_id  = sid,
                product_id = pid,
                name       = $"P-{pid}",
                description= "demo",
                price      = price,
                version    = ver,
                active     = true
            };

        [Fact]
        public async Task SetProduct_Should_Persist_State()
        {
            var g = _cluster.GrainFactory.GetGrain<IProductActor>(1, "200");

            await g.SetProduct(BuildProduct(1, 200, 55));

            var p = await g.GetProduct();
            Assert.True(p.active);
            Assert.Equal(55, p.price);
        }

        [Fact]
        public async Task ProcessProductUpdate_Should_Update_All_Fields()
        {
            var g = _cluster.GrainFactory.GetGrain<IProductActor>(2, "201");

            await g.SetProduct(BuildProduct(2, 201, 10, "v1"));

            var upd = BuildProduct(2, 201, 25, "v2");
            await g.ProcessProductUpdate(upd);

            var p = await g.GetProduct();
            Assert.Equal("v2", p.version);
            Assert.Equal(25,  p.price);
        }

        [Fact]
        public async Task ProcessPriceUpdate_Should_Change_Price_Only()
        {
            var g = _cluster.GrainFactory.GetGrain<IProductActor>(3, "202");
            await g.SetProduct(BuildProduct(3, 202, 30));

            await g.ProcessPriceUpdate(new PriceUpdate { price = 35 });

            var p = await g.GetProduct();
            Assert.Equal(35, p.price);
        }

        [Fact]
        public async Task Reset_Should_Set_Version_To_Zero()
        {
            var g = _cluster.GrainFactory.GetGrain<IProductActor>(4, "203");
            await g.SetProduct(BuildProduct(4, 203, 40, "v9"));

            await g.Reset();

            var p = await g.GetProduct();
            Assert.Equal("0", p.version);
        }
    }
}

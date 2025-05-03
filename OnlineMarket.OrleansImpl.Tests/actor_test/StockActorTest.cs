// ──────────────────────────────────────────────────────────────
// OnlineMarket.OrleansImpl.Tests/actor_test/StockActorTest.cs
// ──────────────────────────────────────────────────────────────
#nullable enable
using Xunit;
using Test.Infra;                                 // BaseTest / Fixture
using OnlineMarket.OrleansImpl.Interfaces;        // IStockActor
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;

namespace OnlineMarket.OrleansImpl.Tests.actor_test
{
    [Collection(NonTransactionalClusterCollection.Name)]
    public class StockActorTest : BaseTest
    {
        public StockActorTest(NonTransactionalClusterFixture fx)
            : base(fx.Cluster) { }

        /*───────────── 构造帮助方法 ─────────────*/

        private static StockItem NewItem(int sid,int pid,int qty = 100)
            => new()
            {
                seller_id     = sid,
                product_id    = pid,
                qty_available = qty,
                qty_reserved  = 0,
                version       = "1"
            };

        private static CartItem Cart(int sid,int pid,int q = 1,string ver="1")
            => new()
            {
                SellerId     = sid,
                ProductId    = pid,
                ProductName  = "Demo",
                Quantity     = q,
                UnitPrice    = 10,
                FreightValue = 0,
                Voucher      = 0,
                Version      = ver
            };
        
        private IStockActor Grain(int sellerId, int productId) =>
            _cluster.GrainFactory.GetGrain<IStockActor>(
                sellerId,
                productId.ToString(),                 // keyExtension
                "OnlineMarket.OrleansImpl.Grains.StockActor");

        /*───────────── 1. Set / Get ─────────────*/

        [Fact]
        public async Task SetItem_Then_GetItem()
        {
            
            // var g   = _cluster.GrainFactory.GetGrain<IStockActor>("50", keyExtension:"100");
            var g = Grain(50, 100);
            var src = NewItem(50,100,500);

            await g.SetItem(src);
            var got = await g.GetItem();

            Assert.Equal(500, got.qty_available);
            Assert.Equal("1", got.version);

            await g.Reset();
        }

        /*───────────── 2. AttemptReservation ────*/

        [Fact]
        public async Task AttemptReservation_Success()
        {
            var g = Grain(51,101);
            await g.SetItem(NewItem(51,101,10));

            var status = await g.AttemptReservation(Cart(51,101,3));
            var item   = await g.GetItem();

            Assert.Equal(ItemStatus.IN_STOCK, status);
            Assert.Equal(3, item.qty_reserved);

            await g.Reset();
        }

        [Fact]
        public async Task AttemptReservation_OutOfStock()
        {
            var g = Grain(52, 102);
            await g.SetItem(NewItem(52,102,2));           // 仅 2 件

            var st = await g.AttemptReservation(Cart(52,102,3));   // 要 3 件
            Assert.Equal(ItemStatus.OUT_OF_STOCK, st);

            await g.Reset();
        }

        [Fact]
        public async Task AttemptReservation_VersionMismatch()
        {
            var g = Grain(53, 103);
            await g.SetItem(NewItem(53,103,5));           // version = "1"

            var st = await g.AttemptReservation(Cart(53,103,1,"999"));
            Assert.Equal(ItemStatus.UNAVAILABLE, st);

            await g.Reset();
        }

        /*───────────── 3. Cancel / Confirm ───────*/

        [Fact]
        public async Task CancelReservation_Should_Decrease_Reserved()
        {
            var g =Grain(54, 104);
            await g.SetItem(NewItem(54,104,10));
            await g.AttemptReservation(Cart(54,104,4));

            await g.CancelReservation(2);        // 取消 2 件
            var st = await g.GetItem();
            Assert.Equal(2, st.qty_reserved);

            await g.Reset();
        }

        [Fact]
        public async Task ConfirmReservation_Should_Move_To_Available()
        {
            var g = Grain(55, 105);
            await g.SetItem(NewItem(55,105,10));
            await g.AttemptReservation(Cart(55,105,4));

            await g.ConfirmReservation(4);
            var it = await g.GetItem();
            Assert.Equal(0 , it.qty_reserved);
            Assert.Equal(6 , it.qty_available);   // 10‑4

            await g.Reset();
        }

        /*───────────── 4. ProductUpdate ──────────*/

        [Fact]
        public async Task ProcessProductUpdate_Should_Change_Version()
        {
            var g = Grain(56, 106);
            await g.SetItem(NewItem(56,106,20));

            await g.ProcessProductUpdate(new ProductUpdated(56,106,"xyz"));
            var it = await g.GetItem();

            Assert.Equal("xyz", it.version);
            await g.Reset();
        }

        /*───────────── 5. Reset ────────────────*/

        [Fact]
        public async Task Reset_Should_Restore_Defaults()
        {
            var g = Grain(57, 107);
            await g.SetItem(NewItem(57,107,5));
            await g.AttemptReservation(Cart(57,107,3));

            await g.Reset();
            var it = await g.GetItem();

            Assert.Equal(0,     it.qty_reserved);
            Assert.Equal(10000, it.qty_available);
            Assert.Equal("0",   it.version);
        }
    }
}

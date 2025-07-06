using Xunit;
using Test.Infra;
using OnlineMarket.OrleansImpl.Interfaces;      // ICartActor / IStockActor / IOrderActor
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;

namespace OnlineMarket.OrleansImpl.Tests.actor_test
{
    [Collection(NonTransactionalClusterCollection.Name)]
    public sealed class CartActorTest : BaseTest
    {
        public CartActorTest(NonTransactionalClusterFixture f) : base(f.Cluster) { }

        /*──── Help Constructor ────*/
        private static CartItem Item(int sid,int pid,string ver="1") => new()
        {
            SellerId     = sid,
            ProductId    = pid,
            ProductName  = "Demo",
            UnitPrice    = 10,
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
            instanceId  = tid,               // Cart will use it as a historical key
            PaymentType = PaymentType.BOLETO.ToString()
        };

        /*──────────── TEST ────────────*/
        [Fact]
        public async Task Cart_Full_Checkout_Should_Create_Order_And_Reserve_Stock()
        {
            int cid = 4001;                 // customer
            int sid = 91;                   // seller
            int pid = 501;                  // product
            string version = "v1";

            /* ① Prepare the inventory first —— StockActor.SetItem */
            var stock = _cluster.GrainFactory.GetGrain<IStockActor>(sid, pid.ToString());
            await stock.SetItem( Stock(sid, pid, version) );

            /* ② CartActor：AddItem + NotifyCheckout */
            var cart = _cluster.GrainFactory.GetGrain<ICartActor>(cid);
            await cart.AddItem( Item(sid, pid, version) );

            string tid = Guid.NewGuid().ToString();
            await cart.NotifyCheckout( Cust(cid, tid) );

            /* Wait for Orleans persistence & chained calls to complete */
            await Task.Delay(150);

            /* ③ Assert A: Cart is cleared */
            var cartSnap = await cart.GetCart();
            Assert.Empty(cartSnap.items);
            Assert.Equal(CartStatus.OPEN, cartSnap.status);

            /* ④ Assertion B: OrderActor generates 1 new order */
            var order = _cluster.GrainFactory.GetGrain<IOrderActor>(cid);
            var orders = await order.GetOrders();
            Assert.Single(orders);
            //Use FakePaymentGateway
            // Assert.Equal(OrderStatus.INVOICED, orders[0].status);
            //Asynchronous process completed
            Assert.Equal(OrderStatus.READY_FOR_SHIPMENT, orders[0].status);
            
            await Task.Delay(150);
            
            /*
            CartActor.Checkout
            └─➤ StockActor.AttemptReservation   // +1  reservation
                └─➤ OrderActor.BuildInvoice
                └─➤ PaymentGrainGateway.StartPaymentAsync
                └─➤ IPaymentActor.ProcessPayment
                └─➤ PaymentServiceCore.ProcessPaymentAsync
                ├─➤ StockGrainGateway.ConfirmAsync      // -1 reservation, -1 available
                ├─➤ 通知 Seller / Order / Customer
                └─➤ ShipmentGateway.StartShipmentAsync
            */

            /* ⑤ Assertion C: Inventory has been reserved (reserved = 1) */
            var stockSnap = await stock.GetItem();
            Assert.Equal(0,  stockSnap.qty_reserved);
            Assert.Equal(99, stockSnap.qty_available);   // 100 - 1

            /* ⑥Reset */
            await cart.Seal();
            await order.Reset();
            await stock.Reset();
        }
    }
}

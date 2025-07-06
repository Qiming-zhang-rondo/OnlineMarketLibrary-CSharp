// ──────────────────────────────────────────────────────────────
// OnlineMarket.OrleansImpl.Tests/actor_test/SellerActorTest.cs
// ──────────────────────────────────────────────────────────────
using Test.Infra;                                    // 你的 BaseTest & Fixture
using OnlineMarket.OrleansImpl.Interfaces;           // ISellerActor
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
using Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection; // GetRequiredServiceByName<>
// [assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace OnlineMarket.OrleansImpl.Tests.actor_test
{
    
    [Collection(NonTransactionalClusterCollection.Name)]
    public class SellerActorTest : BaseTest
    // public class SellerActorTest : BaseTest, IAsyncLifetime
    {
        public SellerActorTest(NonTransactionalClusterFixture fx)
            : base(fx.Cluster) { }
        
        // // Each test case (including each Theory parameter) will be run once before the test.
        // public async Task InitializeAsync()
        // {
        //     var siloHandle = _cluster.Silos.First();
        //     var store = siloHandle.ServiceProvider
        //         .GetRequiredServiceByName<IMemoryGrainStorage>(
        //             Constants.OrleansStorage);
        //     await store.ClearStateAsync();
        // }
        //
        // public Task DisposeAsync() => Task.CompletedTask;
        
        

        private static InvoiceIssued BuildInvoice(int cid, int oid, int sid)
        {
            var item = new OrderItem
            {
                order_id      = oid,
                seller_id     = sid,
                product_id    = 99,
                product_name  = "Demo",
                unit_price    = 10,
                quantity      = 1,
                total_items   = 10,
                total_amount  = 10,
                freight_value = 2,
                voucher       = 0
            };
            var cust = new CustomerCheckout
            {
                CustomerId = cid, FirstName = "Foo", LastName = "Bar",
                City="Cph", Complement="", Street="Main", ZipCode="0000", State="DK"
            };
            return new InvoiceIssued(cust, oid, $"{cid}-{oid}",
                                     DateTime.UtcNow, 12,
                                     new List<OrderItem>{ item },
                                     Guid.NewGuid().ToString());
        }

        private static ShipmentNotification BuildShipment(int cid,int oid,int sid,
                                                          ShipmentStatus st) =>
            new(cid, oid, DateTime.UtcNow, Guid.NewGuid().ToString(), st, sid);

        /*────────────────── Test ─────────────────*/

        [Fact]
        public async Task SetSeller_And_GetSeller()
        {
            var g = _cluster.GrainFactory.GetGrain<ISellerActor>(50);
            await g.SetSeller(new Seller { id = 50, name = "MyShop" });

            var s = await g.GetSeller();
            Assert.Equal("MyShop", s!.name);
            
            await g.Reset();
            
        }

        [Fact]
        public async Task Invoice_Should_Appear_In_Dashboard()
        {
            int sellerId=51;
            var g   = _cluster.GrainFactory.GetGrain<ISellerActor>(sellerId);
            await g.ProcessNewInvoice(BuildInvoice(1, 1001, sellerId));
        
            var dash = await g.QueryDashboard();
            Assert.Equal(1, dash.SellerView.count_orders);
            Assert.Single(dash.OrderEntries);
            
            await g.Reset();
            
        }

        [Fact]
        public async Task PaymentConfirmed_Should_Update_Status()
        {
            
            int sid = 52, cid = 2, oid = 1002;
            var g = _cluster.GrainFactory.GetGrain<ISellerActor>(sid);
        
            // Simulate a new invoice first
            await g.ProcessNewInvoice(BuildInvoice(cid, oid, sid));
            
            
            // Construct a CustomerCheckout object (this is just an example, you fill in the actual properties)
            var customer = new CustomerCheckout
            {
                CustomerId = cid,
                
            };
        
            // Simulate the "Payment Confirmed" event again
            await g.ProcessPaymentConfirmed(new PaymentConfirmed(
                customer   : customer,
                orderId    : oid,                      
                totalAmount: 123.45f,
                items      : new List<OrderItem>(),
                date       : DateTime.UtcNow,
                instanceId : Guid.NewGuid().ToString()));
        
            // Finally, get the Dashboard and check the order_status
            var dash = await g.QueryDashboard();
            Assert.All(dash.OrderEntries,
                e => Assert.Equal(OrderStatus.PAYMENT_PROCESSED, e.order_status));

            await g.Reset();
        }
        
        [Fact]
        public async Task PaymentFailed_Should_Update_Status()
        {
            int sid = 53, cid = 3, oid = 1003;
            var g = _cluster.GrainFactory.GetGrain<ISellerActor>(sid);
        
            // First, create the invoice cache
            await g.ProcessNewInvoice(BuildInvoice(cid, oid, sid));
        
            // To construct the smallest CustomerCheckout, simply set the CustomerId
            var customer = new CustomerCheckout { CustomerId = cid };
            
            var items = new List<OrderItem>();
        
            // Construct the event: The first parameter is the status string
            var evt = new PaymentFailed(
                status: "insufficient_funds",    
                customer: customer,
                orderId: oid,
                items: items,
                totalAmount: 0f,                 
                instanceId: Guid.NewGuid().ToString()
            );
        
            await g.ProcessPaymentFailed(evt);
        
            var dash = await g.QueryDashboard();
            Assert.All(dash.OrderEntries,
                e => Assert.Equal(OrderStatus.PAYMENT_FAILED, e.order_status));
            
            await g.Reset();
            
        }


        // [Fact]
        [Theory]
        [InlineData(54)]
        [InlineData(60)]
        [InlineData(71)]
        public async Task ShipmentWorkflow_Should_Move_Status_And_Then_Remove(int sid)
        {
            // int sid=54; int cid=4; int oid=1004;
            int cid=4; int oid=1004;
            var g = _cluster.GrainFactory.GetGrain<ISellerActor>(sid);
            await g.ProcessNewInvoice(BuildInvoice(cid, oid, sid));
        
            // approved
            await g.ProcessShipmentNotification(BuildShipment(cid, oid, sid, ShipmentStatus.approved));
            await Task.Delay(1000);
            var dash1 = await g.QueryDashboard();
            Assert.All(dash1.OrderEntries, e => Assert.Equal(PackageStatus.ready_to_ship, e.delivery_status));
        
            // in transit
            await g.ProcessShipmentNotification(BuildShipment(cid, oid, sid, ShipmentStatus.delivery_in_progress));
            await Task.Delay(1000);
            var dash2 = await g.QueryDashboard();
            Assert.All(dash2.OrderEntries, e => Assert.Equal(PackageStatus.shipped, e.delivery_status));
        
            // concluded
            await g.ProcessShipmentNotification(BuildShipment(cid, oid, sid, ShipmentStatus.concluded));
            await Task.Delay(1000);
            var dash3 = await g.QueryDashboard();
            Assert.Empty(dash3.OrderEntries);     // Delete
            
            // await g.Reset();
            
        }

        [Fact]
        public async Task DeliveryNotification_Should_Update_Single_Item()
        {
            int sid=55; int cid=5; int oid=1005;
            var g = _cluster.GrainFactory.GetGrain<ISellerActor>(sid);
            await g.ProcessNewInvoice(BuildInvoice(cid, oid, sid));
            
            await Task.Delay(1000);
            
            var before = await g.QueryDashboard();
            Assert.Single(before.OrderEntries);  
        
            await g.ProcessDeliveryNotification(
                new DeliveryNotification(cid, oid, 1, sid, 99, "Demo",
                                         PackageStatus.delivered,
                                         DateTime.UtcNow, Guid.NewGuid().ToString())
            );
            
            await Task.Delay(1000);
            
            var dash = await g.QueryDashboard();
            Assert.Equal(PackageStatus.delivered, dash.OrderEntries[0].delivery_status);
            // await g.Reset();
        }

        [Fact]
        public async Task Reset_Should_Clear_All_Entries()
        {
            var g = _cluster.GrainFactory.GetGrain<ISellerActor>(56);
            await g.ProcessNewInvoice(BuildInvoice(6, 1006, 56));
        
            await g.Reset();
        
            var dash = await g.QueryDashboard();
            Assert.Equal(0, dash.SellerView.count_orders);
            // await g.Reset();
        }
    }
}

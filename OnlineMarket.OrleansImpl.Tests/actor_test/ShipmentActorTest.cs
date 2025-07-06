// ──────────────────────────────────────────────────────────────
// OnlineMarket.OrleansImpl.Tests/actor_test/ShipmentActorTest.cs
// ──────────────────────────────────────────────────────────────
using Xunit;
using Test.Infra;
using OnlineMarket.OrleansImpl.Interfaces;          // IShipmentActor
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
// using OnlineMarket.OrleansImpl.Tests.Fakes;
using Xunit.Abstractions;

namespace OnlineMarket.OrleansImpl.Tests.actor_test;

[Collection(NonTransactionalClusterCollection.Name)]
public class ShipmentActorTest : BaseTest
{
    private readonly ITestOutputHelper _out;
    
    public ShipmentActorTest(NonTransactionalClusterFixture f, ITestOutputHelper output
    ) : base(f.Cluster)
    {
        _out = output;
    }

    /*─────────────── Help ───────────────*/
    
    private static CustomerCheckout Customer(int cid) =>
        new()
        {
            CustomerId = cid,
            FirstName  = "Foo",
            LastName   = "Bar",
            Street     = "Main",
            Complement = "",
            ZipCode    = "0000",
            City       = "Cph",
            State      = "DK"
        };

    private static CartItem Cart(int sellerId, int productId) =>
        new()
        {
            SellerId     = sellerId,
            ProductId    = productId,
            ProductName  = "Demo",
            UnitPrice    = 10,
            FreightValue = 2,
            Quantity     = 1,
            Voucher      = 0,
            Version      = "1"
        };

    private static ReserveStock Reserve(int cid, int oid, int sid)
    {
        // Only the CartItem list uses the collection initializer; CartItem itself uses an object initializer
        var ci = Cart(sid, 99);
        return new ReserveStock(
            DateTime.UtcNow,
            Customer(cid),
            new List<CartItem> { ci },
            Guid.NewGuid().ToString());
    }

    private static PaymentConfirmed Payment(int cid, int oid, int sid)
    {
        var oi = new OrderItem         
        {
            order_id      = oid,
            seller_id     = sid,
            product_id    = 99,
            product_name  = "Demo",
            quantity      = 1,
            unit_price    = 10,
            total_items   = 10,
            total_amount  = 10,
            freight_value = 2,
            voucher       = 0
        };

        return new PaymentConfirmed(
            customer   : Customer(cid),
            orderId    : oid,
            totalAmount: 10,
            items      : new List<OrderItem> { oi },
            date       : DateTime.UtcNow,
            instanceId : Guid.NewGuid().ToString());
    }
    
    private static List<OrderItem> Items(int sellerId) => new()
    {
        new OrderItem
        {
            order_id = 1, seller_id = sellerId,
            product_id = 99, product_name = "Demo",
            unit_price = 10, quantity = 2,
            total_items = 20, total_amount = 20,
            freight_value = 5
        }
    };

    // private static PaymentConfirmed Payment(int cid,int oid,int sid) =>
    //     new(new CustomerCheckout{ CustomerId = cid },  // customer
    //         oid, 25,                                   // orderId / amount
    //         Items(sid), DateTime.UtcNow,
    //         Guid.NewGuid().ToString());

    /*─────────────── 1. Create Shipment Record ───────────────*/
    [Fact]
    public async Task ProcessShipment_Should_Create_Record()
    {
        int shipActorId = 8001;  // ShipmentActor 的粒度主键
        int cid = 1;             // customerId
        // int oid = 90001;         // orderId
        int sid = 77;            // sellerId

        // /*── 1. Create order ─────────────────*/
        var orderGrain = _cluster.GrainFactory.GetGrain<IOrderActor>(cid);
        await orderGrain.Checkout (Reserve(cid, 0, sid));
        await Task.Delay(50);    
        
        var created = (await orderGrain.GetOrders()).Single();
        int oid = created.id;

        /*── 2. Test ShipmentActor ────────────*/
        var g = _cluster.GrainFactory.GetGrain<IShipmentActor>(shipActorId);
        await g.ProcessShipment(Payment(cid, oid, sid));
        await Task.Delay(40);    // 同样缓冲一下持久化

        /*── 3. Assert ───────────────────────────*/
        var list = await g.GetShipments(cid);
        
        // ### Print List in JSON
        var json = System.Text.Json.JsonSerializer.Serialize(
            list[0],
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
        );
        _out.WriteLine("=== Shipments:");
        _out.WriteLine(json);
        _out.WriteLine("==============");
        
        Assert.Single(list);

        var ship = list[0];
        Assert.Equal(ShipmentStatus.approved, ship.status);
        Assert.Equal(1, ship.package_count);    // Same as the number of CartItem

        await g.Reset();   
    }

    /*─────────────── 2. Update → All delivered and deleted ───────────────*/
    [Fact]
    public async Task UpdateShipment_Should_Deliver_And_Delete()
    {
        int actorId = 8002, cid = 2, sid = 78;
        
        /* ① OrderActor order，get orderId */
        var orderGrain = _cluster.GrainFactory.GetGrain<IOrderActor>(cid);
        await orderGrain.Checkout(Reserve(cid, 0, sid));          
        var created = (await orderGrain.GetOrders()).Single();
        int oid = created.id;     
        
        var g       = _cluster.GrainFactory.GetGrain<IShipmentActor>(actorId);

        await g.ProcessShipment(Payment(cid, oid, sid));
        await Task.Delay(40);

        await g.UpdateShipment(Guid.NewGuid().ToString()); // tid
        await Task.Delay(40);

        var listAfter = await g.GetShipments(cid);
        Assert.Empty(listAfter);   // Delete

        await g.Reset();
    }

    /*─────────────── 3. Reset───────────────*/
    [Fact]
    public async Task Reset_Should_Wipe_State()
    {
        int actorId = 8003, cid = 3, sid = 79;
        
        /* ① OrderActor order，get orderId  */
        var orderGrain = _cluster.GrainFactory.GetGrain<IOrderActor>(cid);
        await orderGrain.Checkout(Reserve(cid, 0, sid));          
        var created = (await orderGrain.GetOrders()).Single();
        int oid = created.id; 
        
        var g       = _cluster.GrainFactory.GetGrain<IShipmentActor>(actorId);

        await g.ProcessShipment(Payment(cid, oid, sid));
        await Task.Delay(30);

        await g.Reset();
        var list = await g.GetShipments(cid);
        Assert.Empty(list);
    }
}

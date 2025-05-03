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

    /*─────────────── 构造测试事件 ───────────────*/
    
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
        // 只有 CartItem 列表才用集合初始器；CartItem 本身用对象初始器
        var ci = Cart(sid, 99);
        return new ReserveStock(
            DateTime.UtcNow,
            Customer(cid),
            new List<CartItem> { ci },
            Guid.NewGuid().ToString());
    }

    private static PaymentConfirmed Payment(int cid, int oid, int sid)
    {
        var oi = new OrderItem          // ← OrderItem 也用对象初始器
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

    /*─────────────── 1. 创建快递记录 ───────────────*/
    [Fact]
    public async Task ProcessShipment_Should_Create_Record()
    {
        int shipActorId = 8001;  // ShipmentActor 的粒度主键
        int cid = 1;             // customerId
        // int oid = 90001;         // orderId
        int sid = 77;            // sellerId

        // /*── 1. 先真正生成订单 ─────────────────*/
        var orderGrain = _cluster.GrainFactory.GetGrain<IOrderActor>(cid);
        await orderGrain.Checkout (Reserve(cid, 0, sid));
        await Task.Delay(50);    // 给 OrderActor 写状态一点时间
        
        var created = (await orderGrain.GetOrders()).Single();
        int oid = created.id;

        /*── 2. 再测试 ShipmentActor ────────────*/
        var g = _cluster.GrainFactory.GetGrain<IShipmentActor>(shipActorId);
        await g.ProcessShipment(Payment(cid, oid, sid));
        await Task.Delay(40);    // 同样缓冲一下持久化

        /*── 3. 断言 ───────────────────────────*/
        var list = await g.GetShipments(cid);
        
        // ### 把整个 List 序列化成 JSON 打印出来
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
        Assert.Equal(1, ship.package_count);    // 与 CartItem 数量一致

        await g.Reset();   // 清理状态，避免并行测试串档
    }

    /*─────────────── 2. 更新 → 全部送达并删除 ───────────────*/
    [Fact]
    public async Task UpdateShipment_Should_Deliver_And_Delete()
    {
        int actorId = 8002, cid = 2, sid = 78;
        
        /* ① 先用 OrderActor 真正下单，拿到 orderId */
        var orderGrain = _cluster.GrainFactory.GetGrain<IOrderActor>(cid);
        await orderGrain.Checkout(Reserve(cid, 0, sid));          // 0 只是占位
        var created = (await orderGrain.GetOrders()).Single();
        int oid = created.id;     
        
        var g       = _cluster.GrainFactory.GetGrain<IShipmentActor>(actorId);

        await g.ProcessShipment(Payment(cid, oid, sid));
        await Task.Delay(40);

        await g.UpdateShipment(Guid.NewGuid().ToString()); // tid
        await Task.Delay(40);

        var listAfter = await g.GetShipments(cid);
        Assert.Empty(listAfter);   // 已被 DoUpdate 删除

        await g.Reset();
    }

    /*─────────────── 3. Reset 清空状态 ───────────────*/
    [Fact]
    public async Task Reset_Should_Wipe_State()
    {
        int actorId = 8003, cid = 3, sid = 79;
        
        /* ① 先用 OrderActor 真正下单，拿到 orderId */
        var orderGrain = _cluster.GrainFactory.GetGrain<IOrderActor>(cid);
        await orderGrain.Checkout(Reserve(cid, 0, sid));          // 0 只是占位
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

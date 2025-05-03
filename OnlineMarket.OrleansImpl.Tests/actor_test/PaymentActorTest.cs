// ───────────────────────────────────────────
// OnlineMarket.OrleansImpl.Tests/actor_test/PaymentActorTest.cs
// ───────────────────────────────────────────
using Xunit;
using Test.Infra;                                // BaseTest & Fixture
using OnlineMarket.OrleansImpl.Interfaces;      // 所有 I*Actor
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
using Microsoft.Extensions.DependencyInjection; 
using Xunit.Abstractions;


namespace OnlineMarket.OrleansImpl.Tests.actor_test;

[Collection(NonTransactionalClusterCollection.Name)]
public sealed class PaymentActorTest : BaseTest
{
    private readonly ITestOutputHelper _out;

    public PaymentActorTest(NonTransactionalClusterFixture f, ITestOutputHelper output     // ← xUnit 会自动帮你传这个进来
    ) : base(f.Cluster)
    {
        _out = output;
    }

    /*─────── 帮助构造器（与前面 Seller/Shipment 测试复用即可） ───────*/
    private static CustomerCheckout Cust(int id) => new()
    {
        CustomerId = id,
        FirstName  = "Foo",
        LastName   = "Bar",
        Street     = "Main", ZipCode="0000",
        City="Cph", State="DK",
        PaymentType = PaymentType.CREDIT_CARD.ToString(),
        CardNumber  = "4111111111111111",
        CardHolderName = "FOO BAR",
        CardExpiration  = "12/30",
        CardBrand = "VISA",
        Installments = 1
    };

    private static List<CartItem> Cart(int sid,int pid)=> new()
    {
        new CartItem{ SellerId=sid, ProductId=pid,
                      ProductName="Demo", UnitPrice=10,
                      FreightValue=2, Quantity=1, Version="1"}
    };

    private static ReserveStock RS(int cid,int oid,int sid)=>
        new(DateTime.UtcNow, Cust(cid), Cart(sid,99), Guid.NewGuid().ToString());

    private static InvoiceIssued Invoice(int cid,int oid,int sid)
    {
        var item = new OrderItem{
            order_id=oid, seller_id=sid, product_id=99,
            product_name="Demo", unit_price=10, quantity=1,
            total_items=10, total_amount=10, freight_value=2
        };
        return new InvoiceIssued(
            Cust(cid), oid, $"{cid}-{oid}", DateTime.UtcNow,
            12, new(){ item }, Guid.NewGuid().ToString());
    }

    /*───────────── 1. Happy‑Path 集成验证 ─────────────*/
    [Fact]
    public async Task PaymentActor_FullWorkflow_Should_Create_Shipment()
    {
        int cid = 21, sid = 88;          // 先别写死 oid
        var order = _cluster.GrainFactory.GetGrain<IOrderActor>(cid);

        /* ① 结账 —— 生成订单 */
        await order.Checkout(RS(cid, /*oid 无所谓*/ 0, sid));

        /* ② 取出真正的订单 ID（仓库自增值） */
        // var created = (await order.GetOrders()).Single();
        // int oid = created.id;            // ← 真实 ID

        // /* ③ 构造发票 / 调用 PaymentActor */
        // var pay = _cluster.GrainFactory.GetGrain<IPaymentActor>(cid);
        // await pay.ProcessPayment(Invoice(cid, oid, sid));

        /* ④ 验证已产生 Shipment 记录 */
        int shipActorId = Helper.GetShipmentActorId(
            cid,
            _cluster.ServiceProvider.GetRequiredService<AppConfig>()
                .NumShipmentActors);

        var ship = _cluster.GrainFactory.GetGrain<IShipmentActor>(shipActorId);
        await Task.Delay(100);           // 等异步链路落库

        var list = await ship.GetShipments(cid);
        
        // ### 把整个 List 序列化成 JSON 打印出来
        var json = System.Text.Json.JsonSerializer.Serialize(
            list,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
        );
        _out.WriteLine("=== Shipments:");
        _out.WriteLine(json);
        _out.WriteLine("==============");
        
        Assert.Single(list);
        Assert.Equal(ShipmentStatus.approved, list[0].status);

        /* ⑤ 清理 */
        await ship.Reset();
        await order.Reset();
    }
}

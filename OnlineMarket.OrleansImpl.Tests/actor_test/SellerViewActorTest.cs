// ──────────────────────────────────────────────────────────────
// OnlineMarket.OrleansImpl.Tests/actor_test/SellerActorTest.cs
// （同时覆盖 SellerActor 与 SellerViewActor）
// ──────────────────────────────────────────────────────────────
using Xunit;
using Test.Infra;                                  // BaseTest + Fixture
using OnlineMarket.OrleansImpl.Interfaces;         // ISellerActor
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlineMarket.OrleansImpl.Infra.SellerDb;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
using Xunit.Abstractions;

namespace OnlineMarket.OrleansImpl.Tests.actor_test;

[Collection(NonTransactionalClusterCollectionWithDb.Name)]   // 同一个 Cluster，串行执行
public class SellerViewActorTest : BaseTest
{
    private readonly ITestOutputHelper _output;
    public SellerViewActorTest(
        NonTransactionalClusterFixtureWithDb fx,
        ITestOutputHelper output
    ) : base(fx.Cluster)
    {
        _output = output;
    }

    

    /*─────────────── 帮助方法 ───────────────*/
    private static InvoiceIssued Invoice(int cid, int oid, int sid)
    {
        var item = new OrderItem
        {
            order_id = oid, seller_id = sid, product_id = 99,
            product_name = "Demo", unit_price = 10, quantity = 1,
            total_items = 10, total_amount = 10, freight_value = 2
        };
        return new InvoiceIssued(
            new CustomerCheckout { CustomerId = cid },
            oid, $"{cid}-{oid}", DateTime.UtcNow, 12,
            new() { item }, Guid.NewGuid().ToString());
    }
    private static ShipmentNotification Ship(int cid,int oid,int sid,ShipmentStatus st) =>
        new(cid, oid, DateTime.UtcNow, Guid.NewGuid().ToString(), st, sid);
    
    /*─────────────────── Ⅱ. SellerViewActor 测试 ───────────────────*/
    
    [Fact]
    public async Task SellerViewActor_Write_Should_Persist_To_Pg()
    {
        
        InitSellerDbContext();
        /* arrange */
        
        var factory = _cluster.ServiceProvider.GetRequiredService<IDbContextFactory<SellerDbContext>>();
        await using var ctx = await factory.CreateDbContextAsync();
        // 确保是同一个 Conn
        // _output.WriteLine("TestCtx uses " + ctx.Database.GetDbConnection().ConnectionString);
        // ctx.Database.EnsureDeleted();
        // ctx.Database.EnsureCreated();
        int sid = 7001, cid = 20, oid = 9101;
        var actor = _cluster.GrainFactory.GetGrain<ISellerViewActor>(
            sid, "OnlineMarket.OrleansImpl.Grains.SellerViewActor");

        /* act */
        await actor.ProcessNewInvoice(Invoice(cid, oid, sid));
        await Task.Delay(100);
        var count = await ctx.OrderEntries.CountAsync();
        Assert.Equal(1, count);   // 期待刚插入一条

        var rows = await ctx.OrderEntries
            .Where(e => e.seller_id == sid && e.order_id == oid)
            .ToListAsync();


        Assert.Single(rows);                    // 插进去 1 行
        Assert.Equal(OrderStatus.INVOICED, rows[0].order_status);
    }

    

    // **关键**：显式指定 GrainClassName，告诉 Orleans 取 SellerViewActor
    private ISellerViewActor ViewGrain(int sellerId) =>
        _cluster.GrainFactory.GetGrain<ISellerViewActor>(
            sellerId, "OnlineMarket.OrleansImpl.Grains.SellerViewActor");

    [Fact]
    public async Task SellerViewActor_NewInvoice_Should_Be_In_View()
    {
        InitSellerDbContext();
        
        int sid = 7001, cid = 20, oid = 9101;
        var g = _cluster.GrainFactory.GetGrain<ISellerViewActor>(
            sid, "OnlineMarket.OrleansImpl.Grains.SellerViewActor");
        // var g   = ViewGrain(sid);

        await g.ProcessNewInvoice(Invoice(cid, oid, sid));
        await Task.Delay(50);                       // 给 EF / WriteState 缓冲
        
        // await g.QueryDashboard();          // ① 第一次调用 → 刷新视图
        await Task.Delay(100); 
        var dash = await g.QueryDashboard();   // ② 真正取数据
        Assert.Single(dash.OrderEntries);
        Assert.Equal(1, dash.SellerView.count_orders);

        await g.Reset();
    }

    [Fact]
    public async Task SellerViewActor_Shipment_Workflow()
    {
        InitSellerDbContext();
        
        int sid = 7002, cid = 21, oid = 9102;
        var g   = ViewGrain(sid);

        // ① 新发票
        await g.ProcessNewInvoice(Invoice(cid, oid, sid));
        await Task.Delay(1000);

        // ② approved  → READY_FOR_SHIPMENT / ready_to_ship
        await g.ProcessShipmentNotification(Ship(cid, oid, sid, ShipmentStatus.approved));
        await Task.Delay(50);
        var dash1 = await g.QueryDashboard();

        Assert.Single(dash1.OrderEntries);
        Assert.Equal(PackageStatus.ready_to_ship , dash1.OrderEntries[0].delivery_status);
        Assert.Equal(OrderStatus.READY_FOR_SHIPMENT, dash1.OrderEntries[0].order_status);

        // ③ in‑progress → IN_TRANSIT / shipped
        await g.ProcessShipmentNotification(Ship(cid, oid, sid, ShipmentStatus.delivery_in_progress));
        await Task.Delay(500);
        var dash2 = await g.QueryDashboard();
        
        Assert.Single(dash2.OrderEntries);
        Assert.Equal(PackageStatus.shipped , dash2.OrderEntries[0].delivery_status);
        Assert.Equal(OrderStatus.IN_TRANSIT, dash2.OrderEntries[0].order_status);

        // ④ concluded  → 条目应被删除
        await g.ProcessShipmentNotification(Ship(cid, oid, sid, ShipmentStatus.concluded));
        var dash3 = await g.QueryDashboard();

        Assert.Empty(dash3.OrderEntries);

        await g.Reset();             // 清理状态，避免串档
    }
}

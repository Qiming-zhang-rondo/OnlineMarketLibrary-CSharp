using Xunit;
using Test.Infra;                           // 你的 BaseTest & Fixture
using OnlineMarket.OrleansImpl.Interfaces; // IOrderActor
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[Collection(NonTransactionalClusterCollection.Name)]
public class OrderActorTest : BaseTest
{
    public OrderActorTest(NonTransactionalClusterFixture f) : base(f.Cluster) { }

    /*—— Construct ReserveStock & CartItem ————*/
    private static ReserveStock RS(int cid,int oid,int sid)
    {
        var cart = new CartItem{
            SellerId=sid, ProductId=99, ProductName="Demo",
            UnitPrice=10, FreightValue=2, Quantity=1, Voucher=0, Version="1"
        };
        return new(DateTime.UtcNow,
                   new CustomerCheckout{ CustomerId=cid },
                   new(){ cart },
                   Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task Checkout_Should_Persist_Order_And_Items()
    {
        int cid=2001,  sid=88;
        var g  = _cluster.GrainFactory.GetGrain<IOrderActor>(cid);

        await g.Checkout(RS(cid, 0, sid));
        await Task.Delay(50);               

        var orders = await g.GetOrders();
        Assert.Single(orders);
        //Invoiced还是Ready_for_shipment
        Assert.Equal(OrderStatus.READY_FOR_SHIPMENT, orders[0].status);

        Assert.Equal(1, await g.GetNumOrders());

        await g.Reset();                    
    }

    [Fact]
    public async Task PaymentEvents_Should_Change_Order_Status()
    {
        //Similarly, oid is auto-incremental and cannot be forced to be set in advance
        int cid=2002, sid=89;
        var g  = _cluster.GrainFactory.GetGrain<IOrderActor>(cid);
        await g.Checkout(RS(cid, 0, sid));
        await Task.Delay(30);
        
        var created = (await g.GetOrders()).Single();
        int oid = created.id;

        /*—Send a PaymentConfirmed —*/
        await g.ProcessPaymentConfirmed(
            new PaymentConfirmed(new CustomerCheckout{CustomerId=cid},
                                 oid, 12, null, DateTime.UtcNow,
                                 Guid.NewGuid().ToString()));
        var order = (await g.GetOrders())[0];
        Assert.Equal(OrderStatus.PAYMENT_PROCESSED, order.status);

        /*— Reset —*/
        await g.Reset();
        Assert.Empty(await g.GetOrders());
    }
}

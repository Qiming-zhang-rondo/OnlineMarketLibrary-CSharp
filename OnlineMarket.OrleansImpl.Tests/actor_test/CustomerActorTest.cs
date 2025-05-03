// OnlineMarket.OrleansImpl.Tests/actor_test/CustomerActorTest.cs
using System;
using System.Threading.Tasks;
using Test.Infra;                              // 你的 BaseTest & Fixture
using OnlineMarket.OrleansImpl.Interfaces;    // ICustomerActor
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
using Xunit;

namespace OnlineMarket.OrleansImpl.Tests.actor_test;

[Collection(NonTransactionalClusterCollection.Name)]
public class CustomerActorTest : BaseTest
{
    public CustomerActorTest(NonTransactionalClusterFixture fx) : base(fx.Cluster) { }

    private static Customer NewCustomer(int id) => new()
    {
        id = id,
        first_name             = $"C-{id}",
        last_name              = "",
        success_payment_count = 0,
        failed_payment_count  = 0,
        delivery_count        = 0
    };

    /*────────── 1. 基本 CRUD ─────────*/
    [Fact]
    public async Task Set_And_Get()
    {
        var g = _cluster.GrainFactory.GetGrain<ICustomerActor>(101);
        await g.SetCustomer(NewCustomer(101));

        var c = await g.GetCustomer();
        // 改成断言 first_name
        Assert.Equal("C-101", c.first_name);
    }

    /*────────── 2. 通知统计 ─────────*/
    [Fact]
    public async Task Counters_Should_Increase()
    {
        var g = _cluster.GrainFactory.GetGrain<ICustomerActor>(102);
        await g.SetCustomer(NewCustomer(102));

        await g.NotifyPaymentConfirmed(new PaymentConfirmed(
            customer   : new(){CustomerId = 102},
            orderId    : 1,
            totalAmount: 10,
            items      : new(),
            date       : DateTime.UtcNow,
            instanceId : Guid.NewGuid().ToString()));

        await g.NotifyPaymentFailed(new PaymentFailed(
            status     :"err",
            customer   : new(){CustomerId = 102},
            orderId    : 1,
            items      : new(),
            totalAmount: 0,
            instanceId : Guid.NewGuid().ToString()));

        await g.NotifyDelivery(new DeliveryNotification(
            customerId:102, orderId:1, packageId:1,
            sellerId:  10, productId: 99, productName:"Demo",
            status:    PackageStatus.delivered,
            deliveryDate: DateTime.UtcNow,
            instanceId:   Guid.NewGuid().ToString()));

        var c = await g.GetCustomer();
        Assert.Equal(1, c.success_payment_count);
        Assert.Equal(1, c.failed_payment_count);
        Assert.Equal(1, c.delivery_count);
    }

    /*────────── 3. Clear 状态 ─────────*/
    [Fact]
    public async Task Clear_Should_Wipe_All()
    {
        var g = _cluster.GrainFactory.GetGrain<ICustomerActor>(103);
        await g.SetCustomer(NewCustomer(103));
        await g.Clear();

        var c = await g.GetCustomer();
        Assert.Equal(0, c.success_payment_count + c.failed_payment_count + c.delivery_count);
    }
}

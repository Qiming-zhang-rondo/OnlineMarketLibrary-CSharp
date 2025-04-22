using Xunit;
using Test.Infra;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
using OnlineMarket.OrleansImpl.Interfaces;

namespace OnlineMarket.OrleansImpl.Tests.actor_test;

[Collection(NonTransactionalClusterCollection.Name)]
public class CartActorTest : BaseTest
{
    public CartActorTest(NonTransactionalClusterFixture fixture) : base(fixture.Cluster) { }

    [Fact]
    public async Task AddItem_Should_Add_Item_To_Cart()
    {
        var customerId = 1;
        var cart = _cluster.GrainFactory.GetGrain<ICartActor>(customerId);

        var item = GenerateCartItem(1, 101);

        await cart.AddItem(item);

        var items = await cart.GetItems();

        Assert.Single(items);
        Assert.Equal(101, items[0].ProductId);
    }

    // [Fact]
    // public async Task NotifyCheckout_Should_Process_CustomerCheckout()
    // {
    //     var customerId = 2;
    //     var cart = _cluster.GrainFactory.GetGrain<ICartActor>(customerId);
    //     var item = GenerateCartItem(1, 102);

    //     await cart.AddItem(item);

    //     var checkout = BuildCustomerCheckout(customerId);
    //     await cart.NotifyCheckout(checkout);

    //     // 没有异常即视为成功（可根据是否清空购物车扩展断言）
    //     var itemsAfter = await cart.GetItems();
    //     Assert.NotNull(itemsAfter);
    // }

    [Fact]
    public async Task Seal_Should_NotThrow()
    {
        var cart = _cluster.GrainFactory.GetGrain<ICartActor>(3);
        await cart.Seal();
        // 没抛异常就算通过
    }

    // [Fact]
    // public async Task GetHistory_Should_Return_History_After_Checkout()
    // {
    //     var customerId = 4;
    //     var cart = _cluster.GrainFactory.GetGrain<ICartActor>(customerId);

    //     await cart.AddItem(GenerateCartItem(1, 103));
    //     await cart.NotifyCheckout(BuildCustomerCheckout(customerId));

    //     var history = await cart.GetHistory(customerId.ToString());

    //     Assert.NotEmpty(history);
    // }
}
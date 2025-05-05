using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Entities;
using Test.Infra;
using OnlineMarket.OrleansImpl.Tests.Infra.Mocks;
using OnlineMarket.OrleansImpl.Tests.Infra;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
using Microsoft.Extensions.DependencyInjection;

namespace OnlineMarket.OrleansImpl.Tests.actor_test;
[Collection(NonTransactionalClusterCollection.Name)]
public class PaymentActorTest : BaseTest
{
    public PaymentActorTest(NonTransactionalClusterFixture fixture) : base(fixture.Cluster) { }

    [Fact]
    public async Task ProcessPayment_TriggersShipmentAndCustomerUpdate()
    {
        // Arrange
        var customerId = 201;
        var orderId = 9001;

        // 设置用户（可复用已有的 InitData 方法）
        await InitData(1, 1); // 1 个 customer，1 个 stock item

        // 构造一个虚假的发票（包含商品、总价等）
        var invoice = FakeInvoiceIssued.Create(customerId, orderId);

        var paymentGrain = _cluster.GrainFactory.GetGrain<IPaymentActor>(customerId);

        // Act
        await paymentGrain.ProcessPayment(invoice);

        // Assert
        var customer = await _cluster.GrainFactory.GetGrain<ICustomerActor>(customerId).GetCustomer();
        Assert.Equal(1, customer.success_payment_count);

        var shipments = await _cluster.GrainFactory.GetGrain<IShipmentActor>(
            customerId %  _cluster.ServiceProvider.GetService<OnlineMarket.Core.Common.Config.AppConfig>()!.NumShipmentActors
        ).GetShipments(customerId);
        Assert.Single(shipments);
        Assert.Equal(orderId, shipments[0].order_id);
    }
}
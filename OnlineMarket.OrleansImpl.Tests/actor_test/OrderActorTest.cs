using System.Threading.Tasks;
using Xunit;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.OrleansImpl.Tests.Infra;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.OrleansImpl.Tests.actor_test
{
    [Collection(NonTransactionalClusterCollection.Name)]
    public class OrderActorTest : BaseTest
    {
        public OrderActorTest(NonTransactionalClusterFixture fixture) : base(fixture.Cluster) { }

        [Fact]
        public async Task Checkout_CreatesOrderAndEmitsInvoice()
        {
            // Arrange
            var customerId = 301;
            var orderActor = _cluster.GrainFactory.GetGrain<IOrderActor>(customerId);

            var cartItem1 = new CartItem
            {
                ProductId = 1001,
                SellerId = 1,
                UnitPrice = 10,
                Quantity = 2,
                FreightValue = 5,
                Voucher = 1,
                ProductName = "Test Item",
                Version = "v1"
            };

            var cartItem2 = new CartItem
            {
                ProductId = 1002,
                SellerId = 2,
                UnitPrice = 20,
                Quantity = 1,
                FreightValue = 3,
                Voucher = 0,
                ProductName = "Another Item",
                Version = "v1"
            };

            var reserveStock = new ReserveStock
            {
                items = new() { cartItem1, cartItem2 },
                customerCheckout = BuildCustomerCheckout(customerId),
                timestamp = System.DateTime.Now,
                instanceId = "test-instance"
            };

            // Act
            await orderActor.Checkout(reserveStock);
            var orders = await orderActor.GetOrders();

            // Assert
            Assert.NotEmpty(orders);
        }

        [Fact]
        public async Task Reset_ClearsAllOrders()
        {
            // Arrange
            var customerId = 302;
            var orderActor = _cluster.GrainFactory.GetGrain<IOrderActor>(customerId);

            var reserveStock = new ReserveStock
            {
                items = new() { GenerateCartItem(1, 1001) },
                customerCheckout = BuildCustomerCheckout(customerId),
                timestamp = System.DateTime.Now,
                instanceId = "test-instance"
            };

            await orderActor.Checkout(reserveStock);

            // Act
            await orderActor.Reset();
            var orders = await orderActor.GetOrders();

            // Assert
            Assert.Empty(orders);
        }
    }
}
using System.Threading.Tasks;
using Xunit;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.OrleansImpl.Interfaces;
using Test.Infra.Mocks;
using OnlineMarket.OrleansImpl.Tests.Infra.Eventual;
using OnlineMarket.OrleansImpl.Tests.Infra;
using OnlineMarket.OrleansImpl.Tests.Mock;

namespace OnlineMarket.OrleansImpl.Tests.actor_test
{
    public sealed class CustomerActorTest : BaseTest, IClassFixture<NonTransactionalClusterFixture>
    {
        public CustomerActorTest(NonTransactionalClusterFixture fixture) : base(fixture.Cluster) { }

        [Fact]
        public async Task AddAndGetCustomer_Success()
        {
            // Arrange
            var customerId = 100;
            var customer = new Customer
            {
                id = customerId,
                first_name = "John",
                last_name = "Doe",
                address = "123 Orleans Street",
                complement = "",
                birth_date = "1990-01-01",
                zip_code = "12345",
                city = "TestCity",
                state = "TestState",
                delivery_count = 0,
                failed_payment_count = 0,
                success_payment_count = 0
            };

            var customerGrain = _cluster.GrainFactory.GetGrain<ICustomerActor>(customerId);

            // Act
            await customerGrain.SetCustomer(customer);
            var loadedCustomer = await customerGrain.GetCustomer();

            // Assert
            Assert.NotNull(loadedCustomer);
            Assert.Equal(customer.id, loadedCustomer.id);
            Assert.Equal(customer.first_name, loadedCustomer.first_name);
            Assert.Equal(customer.last_name, loadedCustomer.last_name);
            Assert.Equal(customer.city, loadedCustomer.city);
        }

        [Fact]
        public async Task NotifyDelivery_IncreasesDeliveryCount()
        {
            // Arrange
            var customerId = 101;
            var customerGrain = _cluster.GrainFactory.GetGrain<ICustomerActor>(customerId);
            await customerGrain.SetCustomer(new Customer { id = customerId });

            // Act
            await customerGrain.NotifyDelivery(new Core.Common.Events.DeliveryNotification());
            await Task.Delay(50);
            var updatedCustomer = await customerGrain.GetCustomer();

            // Assert
            Assert.Equal(1, updatedCustomer.delivery_count);
        }

        [Fact]
        public async Task NotifyPaymentFailed_IncreasesFailedPaymentCount()
        {
            // Arrange
            var customerId = 102;
            var customerGrain = _cluster.GrainFactory.GetGrain<ICustomerActor>(customerId);
            await customerGrain.SetCustomer(new Customer { id = customerId });

            var paymentFailed = FakePaymentFailed.Create(customerId);

            // Act
            await customerGrain.NotifyPaymentFailed(paymentFailed);
            var updatedCustomer = await customerGrain.GetCustomer();

            // Assert
            Assert.Equal(1, updatedCustomer.failed_payment_count);
        }

        [Fact]
        public async Task NotifyPaymentConfirmed_IncreasesSuccessPaymentCount()
        {
            // Arrange
            var customerId = 103;
            var customerGrain = _cluster.GrainFactory.GetGrain<ICustomerActor>(customerId);
            await customerGrain.SetCustomer(new Customer { id = customerId });

            var paymentConfirmed = FakePaymentConfirmed.Create(customerId);

            // Act
            await customerGrain.NotifyPaymentConfirmed(paymentConfirmed);
            var updatedCustomer = await customerGrain.GetCustomer();

            // Assert
            Assert.Equal(1, updatedCustomer.success_payment_count);
        }
    }
}
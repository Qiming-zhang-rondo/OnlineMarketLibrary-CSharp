using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.OrleansImpl.Interfaces;


namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public class CustomerActorAdapter : ICustomerService
    {
        private readonly int customerId;
        private readonly IGrainFactory grainFactory;

        public CustomerActorAdapter(int customerId, IGrainFactory grainFactory)
        {
            this.customerId = customerId;
            this.grainFactory = grainFactory;
        }

        private ICustomerActor GetCustomerActor()
        {
            return grainFactory.GetGrain<ICustomerActor>(customerId);
        }

        public Task SetCustomer(Customer customer)
        {
            return GetCustomerActor().SetCustomer(customer);
        }

        public async Task<Customer> GetCustomer()
        {
            return await GetCustomerActor().GetCustomer();
        }

        public Task Clear()
        {
            return GetCustomerActor().Clear();
        }

        public Task NotifyDelivery(DeliveryNotification deliveryNotification)
        {
            return GetCustomerActor().NotifyDelivery(deliveryNotification);
        }

        public Task NotifyPaymentFailed(PaymentFailed paymentFailed)
        {
            return GetCustomerActor().NotifyPaymentFailed(paymentFailed);
        }

        public Task NotifyPaymentConfirmed(PaymentConfirmed paymentConfirmed)
        {
            return GetCustomerActor().NotifyPaymentConfirmed(paymentConfirmed);
        }
    }
}
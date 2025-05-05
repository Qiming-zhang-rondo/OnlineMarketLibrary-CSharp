using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Interfaces;

    public interface ICustomerService
    {
        Task SetCustomer(Customer customer);

        Task Clear();

        Task<Customer> GetCustomer();

        Task NotifyPaymentConfirmed(PaymentConfirmed paymentConfirmed);

        Task NotifyPaymentFailed(PaymentFailed paymentFailed);

        Task NotifyDelivery(DeliveryNotification deliveryNotification);
    }

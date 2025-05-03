using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using Orleans.Concurrency;

namespace OnlineMarket.OrleansImpl.Interfaces;

public interface ICustomerActor : IGrainWithIntegerKey
{
    // API
    Task SetCustomer(Customer customer);

    Task Clear();

    [ReadOnly]
    Task<Customer> GetCustomer();

    [OneWay]
    Task NotifyPaymentConfirmed(PaymentConfirmed paymentConfirmed);

    [OneWay]
    Task NotifyPaymentFailed(PaymentFailed paymentFailed);

    [OneWay]
    Task NotifyDelivery(DeliveryNotification deliveryNotification);
    
}
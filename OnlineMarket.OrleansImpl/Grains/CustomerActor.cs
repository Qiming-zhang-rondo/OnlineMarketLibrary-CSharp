using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra;
using Orleans;
using Orleans.Runtime;
using OnlineMarket.Core.Common.Config;

namespace OnlineMarket.OrleansImpl.Grains;

public class CustomerActor : Grain, ICustomerActor
{
    private readonly ILogger<CustomerServiceCore> logger;
    private readonly IPersistentState<Customer> customerState;
    private readonly AppConfig config;

    private CustomerServiceCore customerService = null!;
    private int customerId;

    public CustomerActor(
        [PersistentState("customer", Constants.OrleansStorage)] IPersistentState<Customer> customerState,
        AppConfig config,
        ILogger<CustomerServiceCore> logger)
    {
        this.customerState = customerState;
        this.logger = logger;
        this.config = config;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        customerId = (int)this.GetPrimaryKeyLong();

        if (customerState.State is null || customerState.State.id == 0)
        {
            customerState.State = new Customer
            {
                id = customerId
            };
        }

        customerService = new CustomerServiceCore(
            logger,
            async () => await customerState.WriteStateAsync()
        );

        return Task.CompletedTask;
    }

    public Task SetCustomer(Customer customer)
    {
        return customerService.SetCustomer(customer);
    }

    public Task<Customer> GetCustomer()
    {
        return customerService.GetCustomer();
    }

    public Task Clear()
    {
        return customerService.Clear();
    }

    public Task NotifyDelivery(DeliveryNotification deliveryNotification)
    {
        return customerService.NotifyDelivery(deliveryNotification);
    }

    public Task NotifyPaymentFailed(PaymentFailed paymentFailed)
    {
        return customerService.NotifyPaymentFailed(paymentFailed);
    }

    public Task NotifyPaymentConfirmed(PaymentConfirmed paymentConfirmed)
    {
        return customerService.NotifyPaymentConfirmed(paymentConfirmed);
    }
}
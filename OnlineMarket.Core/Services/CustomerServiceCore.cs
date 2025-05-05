using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Services;

public sealed class CustomerServiceCore : ICustomerService
{
    private readonly ILogger<CustomerServiceCore> logger;
    private readonly Func<Task> saveCallback;

    private Customer? customer;

    public CustomerServiceCore(
        ILogger<CustomerServiceCore> logger,
        Func<Task> saveCallback)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.saveCallback = saveCallback ?? throw new ArgumentNullException(nameof(saveCallback));
    }

    public async Task SetCustomer(Customer customer)
    {
        this.customer = customer ?? throw new ArgumentNullException(nameof(customer));
        await saveCallback();
    }

    public Task<Customer> GetCustomer()
    {
        if (customer == null)
        {
            throw new InvalidOperationException("Customer is not set.");
        }
        return Task.FromResult(customer);
    }

    public async Task Clear()
    {
        this.customer = null;
        await saveCallback();
    }

    public async Task NotifyDelivery(DeliveryNotification deliveryNotification)
    {
        if (customer == null)
        {
            throw new InvalidOperationException("Customer is not set.");
        }

        customer.delivery_count++;
        await saveCallback();
    }

    public async Task NotifyPaymentFailed(PaymentFailed paymentFailed)
    {
        if (customer == null)
        {
            throw new InvalidOperationException("Customer is not set.");
        }

        customer.failed_payment_count++;
        await saveCallback();
    }

    public async Task NotifyPaymentConfirmed(PaymentConfirmed paymentConfirmed)
    {
        if (customer == null)
        {
            throw new InvalidOperationException("Customer is not set.");
        }

        customer.success_payment_count++;
        await saveCallback();
    }
}
// CustomerServiceCore.cs
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;

namespace OnlineMarket.Core.Services;

public sealed class CustomerServiceCore : ICustomerService
{
    private readonly ICustomerRepository _repo;
    private readonly ILogger             _log;

    private Customer _customer;           // 永远非 null

    public CustomerServiceCore(ICustomerRepository repo, ILogger log)
    {
        _repo = repo  ?? throw new ArgumentNullException(nameof(repo));
        _log  = log   ?? throw new ArgumentNullException(nameof(log));
        _customer = new();                // 确保字段已初始化
    }

    /*───── ICustomerService ─────*/

    public async Task SetCustomer(Customer c)
    {
        _customer = c;
        await _repo.SaveAsync(_customer);
    }

    public async Task Clear()
    {
        _customer = new();
        await _repo.ClearAsync();
    }

    public Task<Customer> GetCustomer() => Task.FromResult(_customer);

    public async Task NotifyPaymentConfirmed(PaymentConfirmed _)
    {
        _customer.success_payment_count++;
        await _repo.SaveAsync(_customer);
    }

    public async Task NotifyPaymentFailed(PaymentFailed _)
    {
        _customer.failed_payment_count++;
        await _repo.SaveAsync(_customer);
    }

    public async Task NotifyDelivery(DeliveryNotification _)
    {
        _customer.delivery_count++;
        await _repo.SaveAsync(_customer);
    }

    /*───── 生命周期 ─────*/
    public async Task LoadAsync() => _customer = await _repo.LoadAsync();
}
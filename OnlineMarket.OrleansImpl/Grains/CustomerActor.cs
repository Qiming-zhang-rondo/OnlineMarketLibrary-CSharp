// OnlineMarket.OrleansImpl.Grains/CustomerActor.cs
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace OnlineMarket.OrleansImpl.Grains;

public sealed class CustomerActor : Grain, ICustomerActor
{
    private readonly IPersistentState<Customer> _state;
    private readonly ILogger<CustomerServiceCore> _log;

    private CustomerServiceCore _svc = null!;

    public CustomerActor(
        [PersistentState("customer", Constants.OrleansStorage)]
        IPersistentState<Customer> state,
        ILogger<CustomerServiceCore> log)
    { _state = state; _log = log; }

    public override async Task OnActivateAsync(CancellationToken _)
    {
        var repo = new OrleansCustomerRepository(_state);
        _svc     = new CustomerServiceCore(repo, _log);
        await _svc.LoadAsync();            //Read Orleans State into Core
    }

    /*──────── ICustomerActor → Core ────────*/
    public Task SetCustomer(Customer c)                     => _svc.SetCustomer(c);
    public Task Clear()                                     => _svc.Clear();
    public Task<Customer> GetCustomer()                     => _svc.GetCustomer();
    public Task NotifyPaymentConfirmed(PaymentConfirmed v)  => _svc.NotifyPaymentConfirmed(v);
    public Task NotifyPaymentFailed(PaymentFailed v)        => _svc.NotifyPaymentFailed(v);
    public Task NotifyDelivery(DeliveryNotification v)      => _svc.NotifyDelivery(v);
}
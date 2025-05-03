// OrleansCustomerRepository.cs   （在 OnlineMarket.OrleansImpl.Infra.Adapter）
using System.Threading.Tasks;
using Orleans.Runtime;
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Common.Entities;

sealed class OrleansCustomerRepository : ICustomerRepository
{
    private readonly IPersistentState<Customer> _state;

    public OrleansCustomerRepository(IPersistentState<Customer> state) => _state = state;

    public Task SaveAsync(Customer c)
    {
        _state.State = c;
        return _state.WriteStateAsync();
    }

    public Task<Customer> LoadAsync()
    {
        _state.State ??= new();   // 第一次激活时建一个空对象
        return Task.FromResult(_state.State);
    }

    public Task ClearAsync() => _state.ClearStateAsync();
}
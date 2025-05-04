// OnlineMarket.OrleansImpl.Infra.Adapter/OrleansCartRepository.cs

using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;
using Orleans.Runtime;

internal sealed class OrleansCartRepository : ICartRepository
{
    private readonly IPersistentState<Cart> _state;
    public OrleansCartRepository(IPersistentState<Cart> st) => _state = st;

    public Task<Cart> LoadAsync(int cid)
    {
        _state.State ??= new Cart(cid);
        return Task.FromResult(_state.State);
    }

    public Task SaveAsync(Cart c)
    {
        _state.State = c;
        return _state.WriteStateAsync();
    }

    public Task ClearAsync(int cid)
    {
        _state.State = new Cart(cid);
        return _state.WriteStateAsync();
    }
}
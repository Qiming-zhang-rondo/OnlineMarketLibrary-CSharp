using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Ports;

// OnlineMarket.Core.Ports/ICartRepository.cs
public interface ICartRepository
{
    Task<Cart>  LoadAsync(int customerId);
    Task SaveAsync(Cart cart);
    Task ClearAsync(int customerId);
}
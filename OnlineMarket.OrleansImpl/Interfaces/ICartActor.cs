using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using Orleans.Concurrency;

namespace OnlineMarket.OrleansImpl.Interfaces;

public interface ICartActor : IGrainWithIntegerKey
{
    Task AddItem(CartItem item);

    [ReadOnly]
    Task<List<CartItem>> GetItems();

    Task NotifyCheckout(CustomerCheckout basketCheckout);

    [ReadOnly]
    Task<Cart> GetCart();

    Task Seal();

    [ReadOnly]
    Task<List<CartItem>> GetHistory(string tid);
}
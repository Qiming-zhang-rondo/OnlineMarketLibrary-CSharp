using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;

namespace OnlineMarket.Core.Interfaces;

public interface ICartService
{
    Task<Cart> GetCart();
    Task<List<CartItem>> GetItems();
    Task AddItem(CartItem item);
    Task NotifyCheckout(CustomerCheckout checkout);
    Task Seal();
    Task<List<CartItem>> GetHistory(string tid);
}
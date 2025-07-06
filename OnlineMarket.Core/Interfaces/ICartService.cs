// OnlineMarket.Core.Interfaces/ICartService.cs
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;

namespace OnlineMarket.Core.Interfaces
{
    /// <summary>
    /// Cart service (no Orleans/EF dependencies)
    /// </summary>
    public interface ICartService
    {
        /*──────── Query - Read Only ────────*/

        /// <summary>Get the current snapshot</summary>
        Task<Cart> GetCartAsync();
        
        Task<IReadOnlyList<CartItem>> GetItemsAsync();

        /// <summary>If trackHistory is enabled, you can use transaction-id to trace back to the snapshot at that time</summary>
        Task<IReadOnlyList<CartItem>> GetHistoryAsync(string tid);

        /*──────── Command —— Change Status ────────*/

        /// <summary>AddItem</summary>
        Task AddItemAsync(CartItem item);

        /// <summary>
        /// User confirms checkout.
        /// Set Cart status to CHECKOUT_SEN
        /// </summary>
        Task NotifyCheckoutAsync(CustomerCheckout checkout);

        /// <summary>Empty the shopping cart and return to OPEN</summary>
        Task SealAsync();

        /// <summary>Reset Cart</summary>
        Task ResetAsync();
    }
}
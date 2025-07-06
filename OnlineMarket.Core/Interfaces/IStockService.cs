// OnlineMarket.Core.Interfaces/IStockService.cs
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;  // 包含 StockItem、CartItem、ItemStatus
using OnlineMarket.Core.Common.Events;    // 包含 ProductUpdated

namespace OnlineMarket.Core.Interfaces
{
    public interface IStockService
    {
        /// Initializes or resets a stock item.
        Task SetItem(StockItem item);
        
        /// Try to reserve stock for cart and return reservation status
        Task<ItemStatus> AttemptReservation(CartItem cartItem);
        
        /// Cancel previous stock reservation
        Task CancelReservation(int quantity);
        
        /// Confirm previous stock reservation
        Task ConfirmReservation(int quantity);
        
        ///product metadata (version number) is updated, update stock item price
        Task ProcessProductUpdate(ProductUpdated productUpdated);
        
        /// Get the current stock item status
        Task<StockItem> GetItem();
        
        /// Reset the stock to its initial state
        Task Reset();
    }
}
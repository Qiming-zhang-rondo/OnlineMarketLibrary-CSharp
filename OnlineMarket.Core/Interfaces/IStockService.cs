using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Interfaces
{
    public interface IStockService
    {
        Task<ItemStatus> AttemptReservation(CartItem cartItem);

        Task CancelReservation(int quantity);

        Task ConfirmReservation(int quantity);

        Task ProcessProductUpdate(ProductUpdated productUpdated);

        Task SetItem(StockItem item);

        Task<StockItem> GetItem();

        Task Reset();
    }
}
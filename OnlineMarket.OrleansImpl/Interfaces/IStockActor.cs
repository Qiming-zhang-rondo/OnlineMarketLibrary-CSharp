using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using Orleans.Concurrency;

namespace OnlineMarket.OrleansImpl.Interfaces;

public interface IStockActor : IGrainWithIntegerCompoundKey
{
    Task<ItemStatus> AttemptReservation(CartItem cartItem);

    Task CancelReservation(int quantity);

    Task ConfirmReservation(int quantity);

    Task ProcessProductUpdate(ProductUpdated productUpdated);

    Task SetItem(StockItem item);

    [ReadOnly]
    Task<StockItem> GetItem();

    Task Reset();
}
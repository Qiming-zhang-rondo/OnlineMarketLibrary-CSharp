using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Ports;

public interface IStockReserver
{
    Task<ItemStatus> TryReserveAsync(CartItem item);
}
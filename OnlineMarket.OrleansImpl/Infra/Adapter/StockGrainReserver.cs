using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

/* StockGrainReserver : IStockReserver */
class StockGrainReserver : IStockReserver
{
    private readonly IGrainFactory _gf;
    public StockGrainReserver(IGrainFactory gf) => _gf = gf;
    public Task<ItemStatus> TryReserveAsync(CartItem c) =>
        _gf.GetGrain<IStockActor>(c.SellerId, c.ProductId.ToString())
            .AttemptReservation(c);
}
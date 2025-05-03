using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

// OnlineMarket.OrleansImpl.Infra.Adapter/StockGrainGateway.cs
public sealed class StockGrainGateway : IStockGateway
{
    private readonly IGrainFactory _gf;
    public StockGrainGateway(IGrainFactory gf) => _gf = gf;

    public Task ConfirmAsync(int sellerId,int productId,int qty) =>
        _gf.GetGrain<IStockActor>(sellerId, productId.ToString())
            .ConfirmReservation(qty);
}
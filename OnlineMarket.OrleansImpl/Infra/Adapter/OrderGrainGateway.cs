// OnlineMarket.OrleansImpl.Infra.Adapter/OrderGrainGateway.cs

using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;

internal sealed class OrderGrainGateway : IOrderGateway
{
    private readonly IGrainFactory _gf;
    public OrderGrainGateway(IGrainFactory gf) => _gf = gf;
    public Task CheckoutAsync(ReserveStock rs) =>
        _gf.GetGrain<IOrderActor>(rs.customerCheckout.CustomerId)
            .Checkout(rs);
}
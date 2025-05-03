using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

public sealed class ShipmentGrainGateway : IShipmentGateway
{
    private readonly IGrainFactory _gf;
    private readonly AppConfig     _cfg;
    public ShipmentGrainGateway(IGrainFactory gf, AppConfig cfg)
    { _gf = gf; _cfg = cfg; }

    public Task StartShipmentAsync(PaymentConfirmed v)
    {
        int id = Helper.GetShipmentActorId(v.customer.CustomerId,
            _cfg.NumShipmentActors);
        return _gf.GetGrain<IShipmentActor>(id).ProcessShipment(v);
    }
}

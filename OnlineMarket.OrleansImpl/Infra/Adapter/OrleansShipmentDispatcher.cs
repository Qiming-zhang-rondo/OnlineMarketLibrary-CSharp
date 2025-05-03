using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

public sealed class OrleansShipmentDispatcher : IShipmentBus
{
    private readonly IGrainFactory _gf;
    private readonly bool _useTxn;

    public OrleansShipmentDispatcher(IGrainFactory gf, bool useTxn)
        => (_gf, _useTxn) = (gf, useTxn);

    public Task DispatchAsync(int pid, string tid,
        ISet<(int customerId, int orderId, int sellerId)> set)
    {
        // if (_useTxn)
        //     return _gf.GetGrain<ITransactionalShipmentActor>(pid)
        //         .UpdateShipment(tid, set);
        // else
        return _gf.GetGrain<IShipmentActor>(pid)
            .UpdateShipment(tid, set);
    }
}

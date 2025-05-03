using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using Orleans.Concurrency;

namespace OnlineMarket.OrleansImpl.Interfaces;

public interface IShipmentActor : IGrainWithIntegerKey
{
    [ReadOnly]
    Task<List<Shipment>> GetShipments(int customerId);

    Task ProcessShipment(PaymentConfirmed paymentConfirmed);

    Task UpdateShipment(string tid);

    Task UpdateShipment(string tid, ISet<(int customerId, int orderId, int sellerId)> entries);

    Task Reset();
}
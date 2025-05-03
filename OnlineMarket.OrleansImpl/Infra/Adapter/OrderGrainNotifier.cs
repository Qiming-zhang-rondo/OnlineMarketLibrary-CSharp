using System.Threading.Tasks;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Ports;
using Orleans;
using OnlineMarket.OrleansImpl.Interfaces; 

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

public class OrderGrainNotifier : IOrderNotifier
{
    private readonly IGrainFactory _factory;
    public OrderGrainNotifier(IGrainFactory factory) => _factory = factory;

    private IOrderActor Get(int customerId) => _factory.GetGrain<IOrderActor>(customerId);

    public Task NotifyShipment(ShipmentNotification n)
    {
        return Get(n.CustomerId).ProcessShipmentNotification(n);
    }
    public Task NotifyPaymentAsync (PaymentConfirmed v) =>
        _factory.GetGrain<IOrderActor>(v.customer.CustomerId)
            .ProcessPaymentConfirmed(v);
}
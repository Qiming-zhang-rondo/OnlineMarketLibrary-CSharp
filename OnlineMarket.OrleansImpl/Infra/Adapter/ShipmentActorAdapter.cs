using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public class ShipmentActorAdapter : IShipmentService
    {
        private readonly int shipmentActorId;
        private readonly IGrainFactory grainFactory;

        public ShipmentActorAdapter(int shipmentActorId, IGrainFactory grainFactory)
        {
            this.shipmentActorId = shipmentActorId;
            this.grainFactory = grainFactory;
        }

        private IShipmentActor GetShipmentActor()
        {
            return grainFactory.GetGrain<IShipmentActor>(shipmentActorId);
        }

        public Task<List<Shipment>> GetShipments(int customerId)
        {
            return GetShipmentActor().GetShipments(customerId);
        }

        public Task ProcessShipment(PaymentConfirmed paymentConfirmed)
        {
            return GetShipmentActor().ProcessShipment(paymentConfirmed);
        }

        public Task UpdateShipment(string tid)
        {
            return GetShipmentActor().UpdateShipment(tid);
        }

        public Task UpdateShipment(string tid, ISet<(int customerId, int orderId, int sellerId)> entries)
        {
            return GetShipmentActor().UpdateShipment(tid, entries);
        }

        public Task Reset()
        {
            return GetShipmentActor().Reset();
        }
    }
}
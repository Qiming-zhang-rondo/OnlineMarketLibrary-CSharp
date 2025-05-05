using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Interfaces
{
    public interface IShipmentService
    {
        Task<List<Shipment>> GetShipments(int customerId);

        Task ProcessShipment(PaymentConfirmed paymentConfirmed);

        Task UpdateShipment(string tid);

        Task UpdateShipment(string tid, ISet<(int customerId, int orderId, int sellerId)> entries);

        Task Reset();
    }
}
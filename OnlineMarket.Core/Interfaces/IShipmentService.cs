// OnlineMarket.Core.Interfaces/IShipmentService.cs
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Interfaces
{
    public interface IShipmentService
    {
        Task ProcessShipment(PaymentConfirmed evt);
        Task UpdateShipment(string tid);
        Task UpdateShipment(string tid, ISet<(int customerId, int orderId, int sellerId)> entries);
        Task<List<Shipment>> GetShipments(int customerId);
        Task Reset();
    }
}
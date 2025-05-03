namespace OnlineMarket.Core.Ports;
using OnlineMarket.Core.Common.Entities;

public interface IShipmentRepository
{
    Task<int>      GetNextIdAsync();
    Task SaveAsync(int id, Shipment shipment, List<Package> packages);
    Task<(Shipment, List<Package>)> LoadAsync(int id);
    Task DeleteAsync(int id);
    Task<List<Shipment>> QueryByCustomerAsync(int customerId);
    Task<Dictionary<int,int>> OldestOpenPerSellerAsync(int take);
    Task ResetAsync();
}
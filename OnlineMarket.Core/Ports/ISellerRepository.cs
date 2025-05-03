using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Ports;

public interface ISellerRepository
{
    Task<Seller?>   LoadSellerAsync(int sellerId);
    Task SaveSellerAsync(Seller seller);

    Task<IDictionary<string, List<OrderEntry>>> LoadEntriesAsync(int sellerId);
    Task SaveEntriesAsync(IDictionary<string, List<OrderEntry>> entries);

    Task ResetAsync(int sellerId);
}
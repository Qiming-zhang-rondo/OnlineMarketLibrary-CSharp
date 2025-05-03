using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Ports;

// Ports/IOrderEntryViewRepository.cs
public interface IOrderEntryViewRepository
{
    Task<IDictionary<(int cid,int oid), List<int>>> LoadCacheAsync(int sellerId);
    Task SaveCacheAsync(IDictionary<(int,int),List<int>> cache);

    Task AddEntriesAsync(IEnumerable<OrderEntry> entries);    // INSERT 新行
    Task UpdateEntriesAsync(IEnumerable<OrderEntry> entries); // UPDATE/DELETE

    Task<IList<OrderEntry>> QueryEntriesBySellerAsync(int sellerId);
    Task ResetAsync(int sellerId);
}


using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.Core.Interfaces.Replication
{
    public interface IReplicaQueryService
    {
        Task<ProductReplica> GetReplicaItem(int sellerId, int productId);
    }
}
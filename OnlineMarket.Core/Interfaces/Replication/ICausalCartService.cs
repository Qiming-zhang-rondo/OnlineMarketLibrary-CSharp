using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.Core.Interfaces.Replication
{
    public interface ICausalCartService : ICartService
    {
        Task<ProductReplica> GetReplicaItem(int sellerId, int productId);
    }
}
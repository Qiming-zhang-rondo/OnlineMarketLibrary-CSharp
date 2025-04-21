
using OnlineMarket.Core.Common.Entities;


namespace OnlineMarket.Core.Interfaces.Replication
{
    public interface IEventualCartService : ICartService
    {
        Task<Product> GetReplicaItem(int sellerId, int productId);
    }
}
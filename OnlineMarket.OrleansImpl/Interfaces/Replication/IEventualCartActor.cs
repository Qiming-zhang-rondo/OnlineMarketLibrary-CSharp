namespace OnlineMarket.OrleansImpl.Interfaces.Replication
{
    using OnlineMarket.Core.Common.Entities;
    using OnlineMarket.OrleansImpl.Interfaces;
    using System.Threading.Tasks;
    using OnlineMarket.Core.Common.Integration;

    public interface IEventualCartActor : ICartActor
    {
        Task<Product> GetReplicaItem(int sellerId, int productId);
    }
}
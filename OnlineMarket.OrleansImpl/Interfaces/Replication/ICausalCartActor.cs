namespace OnlineMarket.OrleansImpl.Interfaces.Replication
{
    using OnlineMarket.Core.Common.Entities;
    using OnlineMarket.Core.Common.Requests;
    using OnlineMarket.OrleansImpl.Interfaces;
    using OnlineMarket.Core.Common.Integration;
    using Orleans.Concurrency;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public interface ICausalCartActor : ICartActor
    {
        Task<ProductReplica> GetReplicaItem(int sellerId, int productId);
    }
}
using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.Core.Ports;

using OnlineMarket.Core.Common.Entities;

public interface IProductReplicaGateway
{
    /// Read the current price and version from the replica storage based on the seller + product ID
    Task<ProductReplica?> GetReplicaAsync(int sellerId, int productId);
}

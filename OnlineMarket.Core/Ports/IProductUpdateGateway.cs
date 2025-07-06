namespace OnlineMarket.Core.Ports;

// OnlineMarket.Core.Ports/IProductUpdateGateway.cs
using OnlineMarket.Core.Common.Entities;

public interface IProductUpdateGateway
{
    /// Subscribe to the update stream of a single product. 
    Task SubscribeAsync(
        int sellerId,
        int productId,
        Func<Product,Task> onChanged);

    /// Cancel all current subscriptions (for resource recovery after checkout)
    Task UnsubscribeAllAsync();
}

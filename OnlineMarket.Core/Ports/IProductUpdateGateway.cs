namespace OnlineMarket.Core.Ports;

// OnlineMarket.Core.Ports/IProductUpdateGateway.cs
using OnlineMarket.Core.Common.Entities;

public interface IProductUpdateGateway
{
    /// 订阅单个商品的更新流。返回订阅句柄，用于后续取消。
    Task SubscribeAsync(
        int sellerId,
        int productId,
        Func<Product,Task> onChanged);

    /// 取消全部当前订阅（用于结账后回收资源）
    Task UnsubscribeAllAsync();
}

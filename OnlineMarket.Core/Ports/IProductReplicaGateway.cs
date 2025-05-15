using OnlineMarket.Core.Common.Integration;

namespace OnlineMarket.Core.Ports;

using OnlineMarket.Core.Common.Entities;

public interface IProductReplicaGateway
{
    /// <summary>
    /// 根据卖家 + 商品 ID 从副本存储中读取当前价格与版本。
    /// 返回 null 表示该商品已不存在（被删除或下架）。
    /// </summary>
    Task<ProductReplica?> GetReplicaAsync(int sellerId, int productId);
}

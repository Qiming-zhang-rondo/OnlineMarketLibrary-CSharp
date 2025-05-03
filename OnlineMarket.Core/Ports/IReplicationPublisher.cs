// Ports/IReplicationPublisher.cs
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Ports
{
    /// <summary>复制 / 缓存端口：将最新商品状态推到消息流、Redis 等。</summary>
    public interface IReplicationPublisher
    {
        Task PublishAsync(Product product);       // 例如 Kafka / Orleans Stream
        Task SaveSnapshotAsync(Product product);  // 例如 Redis / MemoryCache
    }
}
// Ports/IReplicationPublisher.cs
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Ports
{
    ///Replication/cache port: pushes the latest product status to the message stream, Redis
    public interface IReplicationPublisher
    {
        Task PublishAsync(Product product);       // 例如 Kafka / Orleans Stream
        Task SaveSnapshotAsync(Product product);  // 例如 Redis / MemoryCache
    }
}
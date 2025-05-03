// Adapter/NullReplicationPublisher.cs
//Null 实现（不开任何复制时用）
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public sealed class NullReplicationPublisher : IReplicationPublisher
    {
        public Task PublishAsync(Product _)      => Task.CompletedTask;
        public Task SaveSnapshotAsync(Product _) => Task.CompletedTask;
    }
}
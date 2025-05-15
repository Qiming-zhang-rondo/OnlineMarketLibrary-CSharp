// Adapter/CompositeReplicationPublisher.cs
// 组合实现（Stream + Redis 同时开启时用）
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public sealed class CompositeReplicationPublisher : IReplicationPublisher
    {
        private readonly IReplicationPublisher[] _inner;
        public CompositeReplicationPublisher(params IReplicationPublisher[] inner) => _inner = inner;

        public Task PublishAsync(Product p)      => Task.WhenAll(_inner.Select(i => i.PublishAsync(p)));
        public Task SaveSnapshotAsync(Product p) => Task.WhenAll(_inner.Select(i => i.SaveSnapshotAsync(p)));
    }
}
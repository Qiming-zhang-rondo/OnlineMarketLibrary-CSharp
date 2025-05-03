// Adapter/CompositeReplicationPublisher.cs
// 组合实现（Stream + Redis 同时开启时用）
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public sealed class CompositeReplicationPublisher : IReplicationPublisher
    {
        private readonly IReplicationPublisher _a, _b;
        public CompositeReplicationPublisher(IReplicationPublisher a, IReplicationPublisher b)
        {
            _a = a; _b = b;
        }

        public async Task PublishAsync(Product p)
        {
            await _a.PublishAsync(p);
            await _b.PublishAsync(p);
        }

        public async Task SaveSnapshotAsync(Product p)
        {
            await _a.SaveSnapshotAsync(p);
            await _b.SaveSnapshotAsync(p);
        }
    }
}
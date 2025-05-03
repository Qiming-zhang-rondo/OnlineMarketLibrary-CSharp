// Adapter/StreamReplicationPublisher.cs
//Orleans Stream 实现
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;
using Orleans.Streams;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public sealed class StreamReplicationPublisher : IReplicationPublisher
    {
        private readonly IAsyncStream<Product> _stream;
        public StreamReplicationPublisher(IAsyncStream<Product> stream) => _stream = stream;

        public Task PublishAsync(Product product)      => _stream.OnNextAsync(product);
        public Task SaveSnapshotAsync(Product _)       => Task.CompletedTask;
    }
}
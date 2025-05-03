// Adapter/ReplicationBuilder.cs
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Entities;
using Orleans;
using Orleans.Streams;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public static class ReplicationBuilder
    {
        public static IReplicationPublisher Build(
            Grain grain,
            int sellerId,
            int productId,
            AppConfig cfg)
        {
            IReplicationPublisher pub = new NullReplicationPublisher();

            if (cfg.StreamReplication)
            {
                var sp = grain.GetStreamProvider(Constants.DefaultStreamProvider);
                var stream = sp.GetStream<Product>(Constants.ProductNameSpace, $"{sellerId}|{productId}");
                pub = new StreamReplicationPublisher(stream);
            }

            // if (cfg.RedisReplication)
            // {
            //     var redis = grain.ServiceProvider.GetService(typeof(IRedisConnectionFactory)) as IRedisConnectionFactory;
            //     var redisPub = new RedisReplicationPublisher(redis);
            //     pub = cfg.StreamReplication
            //         ? new CompositeReplicationPublisher(pub, redisPub)
            //         : redisPub;
            // }

            return pub;
        }
    }
}
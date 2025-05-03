// Adapter/StockGrainNotifier.cs
//库存通知适配器
using System.Threading.Tasks;
using Orleans;
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.OrleansImpl.Interfaces;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public sealed class StockGrainNotifier : IStockNotifier
    {
        private readonly IGrainFactory _factory;
        public StockGrainNotifier(IGrainFactory factory) => _factory = factory;

        public Task NotifyProductUpdated(ProductUpdated evt)
        {
            var stockGrain = _factory.GetGrain<IStockActor>(evt.sellerId, evt.productId.ToString());
            return stockGrain.ProcessProductUpdate(evt);
        }
    }
}
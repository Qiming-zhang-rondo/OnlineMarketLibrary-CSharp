// ──────────────────────────────────────────────────────────────
// OnlineMarket.OrleansImpl.Grains/ProductActor.cs
// ──────────────────────────────────────────────────────────────

using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;          // ← 适配器实现所在命名空间
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans.Runtime;

namespace OnlineMarket.OrleansImpl.Grains
{
    public sealed class ProductActor : Grain, IProductActor
    {
        private readonly IPersistentState<Product> _state;
        private readonly ILogger<ProductServiceCore> _log;
        private readonly AppConfig _cfg;
        
        private ProductServiceCore _svc = null!;

        public ProductActor(
            [PersistentState("product", Constants.OrleansStorage)] IPersistentState<Product> state,
            AppConfig cfg,
            ILogger<ProductServiceCore> log)
        {
            _state = state;
            _log   = log;
            _cfg   = cfg;
        }

        /*───────────────────────────── Grain life cycle ─────────────────────────────*/

        public override Task OnActivateAsync(CancellationToken token)
        {
            // Extract primary key: sellerId exists in long primary key;
            // productId is placed in string extended key
            int sellerId   = (int)this.GetPrimaryKeyLong(out string productIdStr);
            int productId  = int.Parse(productIdStr);
            
            if (_state.State is null || _state.State.product_id == 0)
                _state.State = new Product { seller_id = sellerId, product_id = productId };
            
            
            var repo = new OrleansProductRepository(_state);
            
            var replicator = ReplicationBuilder.Build(
                                grain        : this,
                                sellerId     : sellerId,
                                productId    : productId,
                                cfg          : _cfg);
            
            var stockNotifier = new StockGrainNotifier(this.GrainFactory);
            
            var clock = SystemClock.Instance;  

            _svc = new ProductServiceCore(
                        sellerId,
                        productId,
                        repo,
                        replicator,
                        stockNotifier,
                        clock,
                        _log,
                        enableStreamReplication: _cfg.StreamReplication,
                        enableSnapshot:          _cfg.RedisReplication);

            return Task.CompletedTask;
        }

        /*───────────────────────────── IProductActor Interface ─────────────────────*/

        public Task SetProduct(Product p)                     => _svc.SetProduct(p);
        public Task ProcessProductUpdate(Product p)           => _svc.ProcessProductUpdate(p);
        public Task<Product> GetProduct()                     => _svc.GetProduct();
        public Task ProcessPriceUpdate(PriceUpdate update)    => _svc.ProcessPriceUpdate(update);
        public Task Reset()                                   => _svc.Reset();
    }
}

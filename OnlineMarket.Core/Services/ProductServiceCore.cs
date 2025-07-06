// ──────────────────────────────────────────────────────────────
// OnlineMarket.Core.Services/ProductServiceCore.cs
// ──────────────────────────────────────────────────────────────
using System;
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;          
using Microsoft.Extensions.Logging;

namespace OnlineMarket.Core.Services
{
    public sealed class ProductServiceCore : IProductService
    {
        // Ports
        private readonly IProductRepository     _repo;        
        private readonly IReplicationPublisher  _replicator;  
        private readonly IStockNotifier         _stock;       
        private readonly IClock                 _clock;       
        private readonly ILogger                _log;

        
        private readonly Product _product;

        // Two optional switches, if Impl does not need them, all can be passed as false
        private readonly bool _enableStreamReplication;
        private readonly bool _enableSnapshot;
        
        public ProductServiceCore(
            int sellerId,
            int productId,
            IProductRepository repo,
            IReplicationPublisher replicator,
            IStockNotifier stock,
            IClock clock,
            ILogger<ProductServiceCore> log,
            bool enableStreamReplication = false,
            bool enableSnapshot          = false)
        {
            _repo         = repo  ?? throw new ArgumentNullException(nameof(repo));
            _replicator   = replicator ?? throw new ArgumentNullException(nameof(replicator));
            _stock        = stock ?? throw new ArgumentNullException(nameof(stock));
            _clock        = clock ?? throw new ArgumentNullException(nameof(clock));
            _log          = log   ?? throw new ArgumentNullException(nameof(log));

            _enableStreamReplication = enableStreamReplication;
            _enableSnapshot          = enableSnapshot;

            _product = new Product
            {
                seller_id  = sellerId,
                product_id = productId,
                active     = false,
                version    = "0"
            };
        }
        
        public async Task SetProduct(Product product)
        {
            if (product is null) throw new ArgumentNullException(nameof(product));

            CopyFrom(product, keepCreatedAt: false);
            _product.active     = true;
            _product.created_at = _clock.UtcNow;

            await PersistAndReplicate();
        }

        public async Task ProcessProductUpdate(Product product)
        {
            if (product is null) throw new ArgumentNullException(nameof(product));

            var createdAtBackup = _product.created_at;

            CopyFrom(product, keepCreatedAt: true);
            _product.created_at = createdAtBackup; // 保留原始创建时间
            _product.updated_at = _clock.UtcNow;

            await _stock.NotifyProductUpdated(
                new ProductUpdated(_product.seller_id, _product.product_id, _product.version));

            await PersistAndReplicate();
        }

        public Task<Product> GetProduct() => Task.FromResult(_product);

        public async Task ProcessPriceUpdate(PriceUpdate priceUpdate)
        {
            if (priceUpdate is null) throw new ArgumentNullException(nameof(priceUpdate));

            _product.price      = priceUpdate.price;
            _product.updated_at = _clock.UtcNow;

            await PersistAndReplicate();
        }

        public async Task Reset()
        {
            _product.version    = "0";
            _product.updated_at = _clock.UtcNow;

            await PersistAndReplicate(publish: false); 
        }

        private void CopyFrom(Product src, bool keepCreatedAt)
        {
            _product.name        = src.name;
            _product.description = src.description;
            _product.price       = src.price;
            _product.version     = src.version;
            _product.active      = src.active;

            if (!keepCreatedAt)
                _product.created_at = src.created_at;
        }

        
        // Unified "write + copy" pipeline
        private async Task PersistAndReplicate(bool publish = true)
        {
            await _repo.SaveAsync(_product);

            try
            {
                if (publish && _enableStreamReplication)
                    await _replicator.PublishAsync(_product);

                if (_enableSnapshot)
                    await _replicator.SaveSnapshotAsync(_product);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Replicating product {Sid}-{Pid} failed.", _product.seller_id, _product.product_id);
                throw;
            }
        }
        
    }
}

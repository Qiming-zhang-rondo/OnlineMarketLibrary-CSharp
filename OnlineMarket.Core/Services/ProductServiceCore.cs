// ──────────────────────────────────────────────────────────────
// OnlineMarket.Core.Services/ProductServiceCore.cs
// ──────────────────────────────────────────────────────────────
using System;
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;          // ← 端口接口：在 Core 内部声明，Impl 中实现
using Microsoft.Extensions.Logging;

namespace OnlineMarket.Core.Services
{
    /// <summary>
    /// 商品领域服务核心实现（纯业务，无任何 Orleans/Redis 等技术耦合）。
    /// </summary>
    public sealed class ProductServiceCore : IProductService
    {
        // 依赖的“端口”（Ports）
        private readonly IProductRepository     _repo;        // 持久化
        private readonly IReplicationPublisher  _replicator;  // 消息流 / Redis 等复制
        private readonly IStockNotifier         _stock;       // 库存通知
        private readonly IClock                 _clock;       // 可测试时钟
        private readonly ILogger                _log;

        /// <summary>商品当前快照；所有方法都直接修改它。</summary>
        private readonly Product _product;

        // 两个可选开关，如果 Impl 不需要可全部传 false
        private readonly bool _enableStreamReplication;
        private readonly bool _enableSnapshot;

        #region ▶ ctor

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

        #endregion

        #region ▶ IProductService 实现

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

            await PersistAndReplicate(publish: false); // 仅快照，不发消息
        }

        #endregion

        #region ▶ 私有辅助

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

        /// <summary>
        /// 统一“写库 + 复制”管道；框架适配逻辑全部托管给 Impl 层。
        /// </summary>
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

        #endregion
    }
}

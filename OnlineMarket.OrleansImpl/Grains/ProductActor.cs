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
    /// <summary>
    /// Orleans‐层的 ProductActor，只做“依赖组装 + 转调”。
    /// </summary>
    public sealed class ProductActor : Grain, IProductActor
    {
        // Orleans 持久化状态
        private readonly IPersistentState<Product> _state;
        private readonly ILogger<ProductServiceCore> _log;
        private readonly AppConfig _cfg;

        // 核心业务服务
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

        /*───────────────────────────── Grain 生命周期 ─────────────────────────────*/

        public override Task OnActivateAsync(CancellationToken token)
        {
            // 1) 提取主键：sellerId 存在 long 主键；productId 放在 string 扩展键
            int sellerId   = (int)this.GetPrimaryKeyLong(out string productIdStr);
            int productId  = int.Parse(productIdStr);

            // 2) 若首次激活且状态为空，初始化一个空 Product
            if (_state.State is null || _state.State.product_id == 0)
                _state.State = new Product { seller_id = sellerId, product_id = productId };

            /* 3) 组装“端口”适配器 －－－－－－－－－－－－－－－－－－－－ */

            // 3-1 持久化仓储（Orleans Storage）
            var repo = new OrleansProductRepository(_state);

            // 3-2 复制通道
            var replicator = ReplicationBuilder.Build(
                                grain        : this,
                                sellerId     : sellerId,
                                productId    : productId,
                                cfg          : _cfg);

            // 3-3 库存通知
            var stockNotifier = new StockGrainNotifier(this.GrainFactory);

            // 3-4 时钟
            var clock = SystemClock.Instance;   // 简单单例实现 IClock

            /* 4) 实例化核心服务 －－－－－－－－－－－－－－－－－－－－－－ */

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

        /*───────────────────────────── IProductActor 接口转调 ─────────────────────*/

        public Task SetProduct(Product p)                     => _svc.SetProduct(p);
        public Task ProcessProductUpdate(Product p)           => _svc.ProcessProductUpdate(p);
        public Task<Product> GetProduct()                     => _svc.GetProduct();
        public Task ProcessPriceUpdate(PriceUpdate update)    => _svc.ProcessPriceUpdate(update);
        public Task Reset()                                   => _svc.Reset();
    }
}

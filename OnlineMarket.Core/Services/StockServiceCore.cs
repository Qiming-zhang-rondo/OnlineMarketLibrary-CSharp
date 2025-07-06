using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;

namespace OnlineMarket.Core.Services
{
    public sealed class StockServiceCore : IStockService
    {
        private readonly IStockRepository _repo;
        private readonly IClock           _clock;
        private readonly ILogger<StockServiceCore> _log;

        private readonly StockItem _item;

        public StockServiceCore(
            int sellerId,
            int productId,
            IStockRepository repo,
            IClock clock,
            ILogger<StockServiceCore> log)
        {
            _repo  = repo;
            _clock = clock;
            _log   = log;

            _item = new StockItem
            {
                seller_id   = sellerId,
                product_id  = productId,
                qty_available = 10000,
                qty_reserved  = 0,
                version       = "0",
            };
        }

        /*────────────── IStockService  ──────────────*/

        public async Task SetItem(StockItem item)
        {
            CopyFrom(item);
            _item.created_at = _clock.UtcNow;
            await _repo.SaveAsync(_item);
        }

        public async Task<ItemStatus> AttemptReservation(CartItem cartItem)
        {
            if (_item.version is null)
            {
                _log.LogError("Stock {0}:{1} version null", _item.seller_id, _item.product_id);
                throw new InvalidOperationException("Version is null");
            }

            if (string.Compare(_item.version, cartItem.Version, StringComparison.Ordinal) != 0)
                return ItemStatus.UNAVAILABLE;

            if (_item.qty_reserved + cartItem.Quantity > _item.qty_available)
            {
                _log.LogWarning("Stock {0}:{1} running out", _item.seller_id, _item.product_id);
                return ItemStatus.OUT_OF_STOCK;
            }

            _item.qty_reserved += cartItem.Quantity;
            _item.updated_at    = _clock.UtcNow;
            await _repo.SaveAsync(_item);
            return ItemStatus.IN_STOCK;
        }

        public async Task CancelReservation(int quantity)
        {
            _item.qty_reserved -= quantity;
            _item.updated_at    = _clock.UtcNow;
            await _repo.SaveAsync(_item);
        }

        public async Task ConfirmReservation(int quantity)
        {
            _item.qty_reserved  -= quantity;
            _item.qty_available -= quantity;
            _item.updated_at     = _clock.UtcNow;
            await _repo.SaveAsync(_item);
        }

        public async Task ProcessProductUpdate(ProductUpdated evt)
        {
            _item.version    = evt.instanceId;
            _item.updated_at = _clock.UtcNow;
            await _repo.SaveAsync(_item);
        }

        public Task<StockItem> GetItem() => Task.FromResult(_item);

        public async Task Reset()
        {
            _item.qty_reserved  = 0;
            _item.qty_available = 10000;
            _item.version       = "0";
            _item.updated_at    = _clock.UtcNow;
            await _repo.SaveAsync(_item);
        }

        /*──────────────── Private ────────────────*/
        private void CopyFrom(StockItem src)
        {
            _item.qty_available = src.qty_available;
            _item.qty_reserved  = src.qty_reserved;
            _item.version       = src.version;
        }
    }
}

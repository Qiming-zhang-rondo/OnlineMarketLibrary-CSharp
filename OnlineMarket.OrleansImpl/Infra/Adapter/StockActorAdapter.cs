using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public class StockActorAdapter : IStockService
    {
        private readonly int sellerId;
        private readonly string productId;
        private readonly IGrainFactory grainFactory;

        public StockActorAdapter(int sellerId, string productId, IGrainFactory grainFactory)
        {
            this.sellerId = sellerId;
            this.productId = productId;
            this.grainFactory = grainFactory;
        }

        private IStockActor GetStockActor()
        {
            return grainFactory.GetGrain<IStockActor>(sellerId, productId);
        }

        public Task<ItemStatus> AttemptReservation(CartItem cartItem)
        {
            return GetStockActor().AttemptReservation(cartItem);
        }

        public Task CancelReservation(int quantity)
        {
            return GetStockActor().CancelReservation(quantity);
        }

        public Task ConfirmReservation(int quantity)
        {
            return GetStockActor().ConfirmReservation(quantity);
        }

        public Task ProcessProductUpdate(ProductUpdated productUpdated)
        {
            return GetStockActor().ProcessProductUpdate(productUpdated);
        }

        public Task SetItem(StockItem item)
        {
            return GetStockActor().SetItem(item);
        }

        public Task<StockItem> GetItem()
        {
            return GetStockActor().GetItem();
        }

        public Task Reset()
        {
            return GetStockActor().Reset();
        }
    }
}
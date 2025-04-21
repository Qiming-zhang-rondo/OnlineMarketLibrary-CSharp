using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Services.Replication;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.OrleansImpl.Interfaces.Replication;
using OnlineMarket.OrleansImpl.Infra;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra.Adapter;

namespace OnlineMarket.OrleansImpl.Grains.Replication
{
    public class EventualCartActor : CartActor, IEventualCartActor
    {
        private IStreamProvider streamProvider = null!;
        private readonly Dictionary<(int SellerId, int ProductId), Product> cachedProducts = new();
        private readonly List<StreamSubscriptionHandle<Product>> consumerHandles = new();

        public EventualCartActor(
    [PersistentState("cart", Constants.OrleansStorage)] IPersistentState<Cart> cartState,
    AppConfig config,
    ILogger<CartServiceCore> logger,
    IStreamProvider streamProvider,
    Dictionary<(int, int), Product> cachedProducts
) : base(cartState, config, logger)
        {
            this.streamProvider = streamProvider;
            this.cachedProducts = cachedProducts;
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            customerId = (int)this.GetPrimaryKeyLong();

            if (cartState.State is null || cartState.State.customerId == 0)
            {
                cartState.State = new Cart(customerId);
            }

            this.streamProvider = this.GetStreamProvider(Constants.DefaultStreamProvider);

            cartService = new EventualCartServiceCore(
                customerId,
                logger,
                new OrderActorAdapter(customerId,GrainFactory),
                async () => await cartState.WriteStateAsync(),
                cachedProducts,
                trackHistory: false
            );

            await base.OnActivateAsync(cancellationToken);
        }

        public async Task BecomeConsumer(string id)
        {
            var stream = streamProvider.GetStream<Product>(Constants.ProductNameSpace, id);
            var handle = await stream.SubscribeAsync(UpdateProductAsync);
            consumerHandles.Add(handle);
        }

        public async Task StopConsuming()
        {
            var tasks = new List<Task>();
            foreach (var handle in consumerHandles)
            {
                tasks.Add(handle.UnsubscribeAsync());
            }
            await Task.WhenAll(tasks);
            consumerHandles.Clear();
        }

        private Task UpdateProductAsync(Product product, StreamSequenceToken? token)
        {
            var key = (product.seller_id, product.product_id);
            cachedProducts[key] = product;
            return Task.CompletedTask;
        }


        public Task<Product> GetReplicaItem(int sellerId, int productId)
        {
            var key = (sellerId, productId);
            return Task.FromResult(cachedProducts[key]);
        }

        public override async Task AddItem(CartItem item)
        {
            await BecomeConsumer($"{item.SellerId}|{item.ProductId}");
            await base.AddItem(item);
        }

        public override async Task NotifyCheckout(CustomerCheckout customerCheckout)
        {
            await base.NotifyCheckout(customerCheckout);
            await StopConsuming();
        }
    }
}
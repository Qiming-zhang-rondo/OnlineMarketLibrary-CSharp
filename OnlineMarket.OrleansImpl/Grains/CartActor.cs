using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra;
using Orleans;
using Orleans.Runtime;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Tests.Infra.Mocks;

namespace OnlineMarket.OrleansImpl.Grains;

public class CartActor : Grain, ICartActor
{
    protected readonly ILogger<CartServiceCore> logger;
    protected readonly IPersistentState<Cart> cartState;

    protected CartServiceCore cartService = null!;
    protected int customerId;
    protected AppConfig config;

    public CartActor(
        [PersistentState("cart", Constants.OrleansStorage)] IPersistentState<Cart> cartState,
        AppConfig config,
        ILogger<CartServiceCore> logger)
    {
        this.cartState = cartState;
        this.logger = logger;
        this.config = config;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        customerId = (int)this.GetPrimaryKeyLong();

        if (cartState.State is null || cartState.State.customerId == 0)
        {
            cartState.State = new Cart(customerId);
        }

        cartService = new CartServiceCore(
            customerId,
            logger,
            // new FakeOrderActorAdapter(),
            new OrderActorAdapter(customerId,GrainFactory),
            async () => await cartState.WriteStateAsync(),
            trackHistory: false
        );

        return Task.CompletedTask;
    }

    public virtual Task<Cart> GetCart() => cartService.GetCart();

    public virtual Task<List<CartItem>> GetItems() => cartService.GetItems();

    public virtual Task AddItem(CartItem item) => cartService.AddItem(item);

    public virtual Task NotifyCheckout(CustomerCheckout basketCheckout) => cartService.NotifyCheckout(basketCheckout);

    public virtual Task Seal() => cartService.Seal();

    public virtual Task<List<CartItem>> GetHistory(string tid) => cartService.GetHistory(tid);
}

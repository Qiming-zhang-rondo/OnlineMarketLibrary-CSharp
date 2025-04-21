using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Events;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Interfaces;

namespace OnlineMarket.Core.Services
{
    public class CartServiceCore : ICartService
    {
        protected readonly ILogger<CartServiceCore> logger;
        protected readonly IOrderService orderService;
        protected readonly Func<Task> saveCallback;
        protected readonly bool trackHistory;

        protected readonly Dictionary<string, List<CartItem>> history = new();
        protected readonly Cart cart;

        public CartServiceCore(int customerId, ILogger<CartServiceCore> logger, IOrderService orderService, Func<Task> saveCallback, bool trackHistory = false)
        {
            this.cart = new Cart(customerId);
            this.logger = logger;
            this.orderService = orderService;
            this.saveCallback = saveCallback;
            this.trackHistory = trackHistory;
        }

        public Task<Cart> GetCart()
        {
            return Task.FromResult(cart);
        }

        public Task<List<CartItem>> GetItems()
        {
            return Task.FromResult(cart.items);
        }

        public async Task AddItem(CartItem item)
        {
            if (item.Quantity <= 0)
                throw new ArgumentException($"Item {item.ProductId} shows no positive quantity.");

            if (cart.status == CartStatus.CHECKOUT_SENT)
                throw new InvalidOperationException($"Cart already sent for checkout.");

            cart.items.Add(item);
            await saveCallback();
        }

        public virtual async Task NotifyCheckout(CustomerCheckout customerCheckout)
        {
            var checkout = new ReserveStock(DateTime.UtcNow, customerCheckout, cart.items, customerCheckout.instanceId);
            cart.status = CartStatus.CHECKOUT_SENT;

            try
            {
                if (trackHistory)
                {
                    history.TryAdd(customerCheckout.instanceId, new List<CartItem>(cart.items));
                }

                await orderService.Checkout(checkout);
                await Seal();
            }
            catch (Exception e)
            {
                var str = $"Checkout exception caught in cart ID {cart.customerId}: {e.StackTrace} - {e.Source} - {e.InnerException} - {e.Data}";
                logger.LogError(str);
                throw new ApplicationException(str);
            }
        }

        public async Task Seal()
        {
            cart.status = CartStatus.OPEN;
            cart.items.Clear();
            await saveCallback();
        }

        public Task<List<CartItem>> GetHistory(string tid)
        {
            var result = history.TryGetValue(tid, out var list) ? list : new List<CartItem>();
            return Task.FromResult(result);
        }
    }
}
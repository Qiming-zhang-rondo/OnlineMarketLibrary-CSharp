using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using Orleans;
using Orleans.Runtime;
using static OnlineMarket.Core.Services.OrderServiceCore;

namespace OnlineMarket.OrleansImpl.Grains
{
    public sealed class OrderActor : Grain, IOrderActor
    {
        private readonly ILogger<OrderServiceCore> logger;
        private readonly IPersistentState<Dictionary<int, OrderState>> orders;
        private readonly IPersistentState<NextOrderIdState> nextOrderId;
        private readonly AppConfig config;

        private OrderServiceCore orderService = null!;
        private int customerId;

        public OrderActor(
            [PersistentState("orders", Constants.OrleansStorage)] IPersistentState<Dictionary<int, OrderState>> orders,
            [PersistentState("nextOrderId", Constants.OrleansStorage)] IPersistentState<NextOrderIdState> nextOrderId,
            AppConfig config,
            ILogger<OrderServiceCore> logger)
        {
            this.orders = orders;
            this.nextOrderId = nextOrderId;
            this.logger = logger;
            this.config = config;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            this.customerId = (int)this.GetPrimaryKeyLong();

            if (orders.State == null)
                orders.State = new();

            if (nextOrderId.State == null)
                nextOrderId.State = new();

            orderService = new OrderServiceCore(
                customerId,
                logger,
                async () => await orders.WriteStateAsync(),
                async () => await nextOrderId.WriteStateAsync(),
                () => nextOrderId.State.Value,
                config,
                tuple => new StockActorAdapter(tuple.sellerId, tuple.productId,GrainFactory),
                custId => new PaymentActorAdapter(custId, GrainFactory),
                sellerId => new SellerActorAdapter(sellerId, GrainFactory),
                groupId => new ShipmentActorAdapter(groupId, GrainFactory)
            );

            return Task.CompletedTask;
        }

        public Task Checkout(ReserveStock reserveStock) => orderService.Checkout(reserveStock);

        public Task ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed) => orderService.ProcessPaymentConfirmed(paymentConfirmed);

        public Task ProcessPaymentFailed(PaymentFailed paymentFailed) => orderService.ProcessPaymentFailed(paymentFailed);

        public Task ProcessShipmentNotification(ShipmentNotification shipmentNotification) => orderService.ProcessShipmentNotification(shipmentNotification);

        public Task<List<Order>> GetOrders() => orderService.GetOrders();

        public Task<int> GetNumOrders() => orderService.GetNumOrders();

        public Task Reset() => orderService.Reset();
    }

  

    public sealed class NextOrderIdState
    {
        public int Value { get; set; } = 0;

        public NextOrderIdState GetNextOrderId()
        {
            this.Value++;
            return this;
        }
    }
}

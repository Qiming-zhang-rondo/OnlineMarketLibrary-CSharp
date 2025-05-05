using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Utils;

namespace OnlineMarket.Core.Services
{
    public sealed class OrderServiceCore : IOrderService
    {
        private readonly int customerId;
        private readonly ILogger<OrderServiceCore> logger;
        private readonly AppConfig config;

        private readonly Func<(int sellerId, string productId), IStockService> getStockService;
        private readonly Func<int, IPaymentService> getPaymentService;
        private readonly Func<int, ISellerService> getSellerService;
        private readonly Func<int, IShipmentService> getShipmentService;

        private readonly Func<Task> saveOrdersCallback;
        private readonly Func<Task> saveNextOrderIdCallback;
        private readonly Func<int> getNextOrderId;

        private readonly Dictionary<int, OrderState> orders = new();

        public OrderServiceCore(
            int customerId,
            ILogger<OrderServiceCore> logger,
            Func<Task> saveOrdersCallback,
            Func<Task> saveNextOrderIdCallback,
            Func<int> getNextOrderId,
            AppConfig config,
            Func<(int sellerId, string productId), IStockService> getStockService,
            Func<int, IPaymentService> getPaymentService,
            Func<int, ISellerService> getSellerService,
            Func<int, IShipmentService> getShipmentService)
        {
            this.customerId = customerId;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.saveOrdersCallback = saveOrdersCallback ?? throw new ArgumentNullException(nameof(saveOrdersCallback));
            this.saveNextOrderIdCallback = saveNextOrderIdCallback ?? throw new ArgumentNullException(nameof(saveNextOrderIdCallback));
            this.getNextOrderId = getNextOrderId ?? throw new ArgumentNullException(nameof(getNextOrderId));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.getStockService = getStockService ?? throw new ArgumentNullException(nameof(getStockService));
            this.getPaymentService = getPaymentService ?? throw new ArgumentNullException(nameof(getPaymentService));
            this.getSellerService = getSellerService ?? throw new ArgumentNullException(nameof(getSellerService));
            this.getShipmentService = getShipmentService ?? throw new ArgumentNullException(nameof(getShipmentService));
        }

        public async Task Checkout(ReserveStock reserveStock)
        {
            var now = DateTime.UtcNow;
            var statusResp = reserveStock.items.Select(item => getStockService((item.SellerId, item.ProductId.ToString())).AttemptReservation(item)).ToList();
            await Task.WhenAll(statusResp);

            var itemsToCheckout = reserveStock.items
                .Where((item, idx) => statusResp[idx].Result == ItemStatus.IN_STOCK)
                .ToList();

            float totalFreight = itemsToCheckout.Sum(item => item.FreightValue);
            float totalAmount = itemsToCheckout.Sum(item => item.UnitPrice * item.Quantity);
            float totalItems = totalAmount;
            float totalIncentive = 0;
            var totalPerItem = new Dictionary<(int, int), float>();

            foreach (var item in itemsToCheckout)
            {
                float totalItem = item.UnitPrice * item.Quantity;

                if (totalItem - item.Voucher > 0)
                {
                    totalAmount -= item.Voucher;
                    totalIncentive += item.Voucher;
                    totalItem -= item.Voucher;
                }
                else
                {
                    totalAmount -= totalItem;
                    totalIncentive += totalItem;
                    totalItem = 0;
                }

                totalPerItem.Add((item.SellerId, item.ProductId), totalItem);
            }

            int orderId = getNextOrderId();
            await saveNextOrderIdCallback();

            var invoiceNumber = InvoiceHelper.GetInvoiceNumber(customerId, now, orderId);

            var order = new Order
            {
                id = orderId,
                customer_id = customerId,
                invoice_number = invoiceNumber,
                status = OrderStatus.INVOICED,
                purchase_date = reserveStock.timestamp,
                total_amount = totalAmount,
                total_items = totalItems,
                total_freight = totalFreight,
                total_incentive = totalIncentive,
                total_invoice = totalAmount + totalFreight,
                count_items = itemsToCheckout.Count,
                created_at = now,
                updated_at = now
            };

            var orderItems = OrderItemMapper.MapFromCartItems(itemsToCheckout, orderId, totalPerItem);
            var orderState = new OrderState
            {
                order = order,
                orderItems = orderItems,
                orderHistory = new List<OrderHistory>
                {
                    new OrderHistory
                    {
                        order_id = orderId,
                        created_at = now,
                        status = OrderStatus.INVOICED
                    }
                }
            };

            orders[orderId] = orderState;
            await saveOrdersCallback();

            var invoice = new InvoiceIssued(
                reserveStock.customerCheckout,
                orderId,
                invoiceNumber,
                now,
                order.total_invoice,
                orderItems,
                reserveStock.instanceId);

            var tasks = new List<Task>();

            var sellerIds = orderItems.Select(x => x.seller_id).Distinct();
            foreach (var sellerId in sellerIds)
            {
                var sellerService = getSellerService(sellerId);
                var sellerInvoice = new InvoiceIssued(
                    reserveStock.customerCheckout,
                    orderId,
                    invoiceNumber,
                    now,
                    order.total_invoice,
                    orderItems.Where(x => x.seller_id == sellerId).ToList(),
                    reserveStock.instanceId);

                tasks.Add(sellerService.ProcessNewInvoice(sellerInvoice));
            }

            var paymentService = getPaymentService(customerId);
            tasks.Add(paymentService.ProcessPayment(invoice));

            await Task.WhenAll(tasks);
        }

        public async Task ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed)
        {
            if (!orders.TryGetValue(paymentConfirmed.orderId, out var orderState))
            {
                logger.LogWarning("Order {OrderId} not found for payment confirmation", paymentConfirmed.orderId);
                return;
            }

            var now = DateTime.UtcNow;
            orderState.orderHistory.Add(new OrderHistory
            {
                order_id = paymentConfirmed.orderId,
                created_at = now,
                status = OrderStatus.PAYMENT_PROCESSED
            });

            orderState.order.status = OrderStatus.PAYMENT_PROCESSED;
            orderState.order.updated_at = now;

            await saveOrdersCallback();
        }

        public async Task ProcessPaymentFailed(PaymentFailed paymentFailed)
        {
            if (!orders.TryGetValue(paymentFailed.orderId, out var orderState))
            {
                logger.LogWarning("Order {OrderId} not found for payment failed", paymentFailed.orderId);
                return;
            }

            orders.Remove(paymentFailed.orderId);
            await saveOrdersCallback();
        }

        public async Task ProcessShipmentNotification(ShipmentNotification shipmentNotification)
        {
            if (!orders.TryGetValue(shipmentNotification.orderId, out var orderState))
            {
                logger.LogWarning("Order {OrderId} not found for shipment notification", shipmentNotification.orderId);
                return;
            }

            var now = DateTime.UtcNow;
            var newStatus = shipmentNotification.status switch
            {
                ShipmentStatus.delivery_in_progress => OrderStatus.IN_TRANSIT,
                ShipmentStatus.concluded => OrderStatus.DELIVERED,
                _ => OrderStatus.READY_FOR_SHIPMENT
            };

            orderState.orderHistory.Add(new OrderHistory
            {
                order_id = shipmentNotification.orderId,
                created_at = now,
                status = newStatus
            });

            orderState.order.status = newStatus;
            orderState.order.updated_at = now;

            if (newStatus == OrderStatus.DELIVERED)
            {
                orderState.order.delivered_customer_date = shipmentNotification.eventDate;
                orders.Remove(shipmentNotification.orderId);
            }

            await saveOrdersCallback();
        }

        public Task<List<Order>> GetOrders()
        {
            var res = orders.Values.Select(x => x.order).ToList();
            return Task.FromResult(res);
        }

        public Task<int> GetNumOrders()
        {
            return Task.FromResult(orders.Count);
        }

        public async Task Reset()
        {
            orders.Clear();
            await saveOrdersCallback();
        }

        public sealed class OrderState
        {
            public Order order { get; set; } = null!;
            public List<OrderItem> orderItems { get; set; } = new List<OrderItem>();
            public List<OrderHistory> orderHistory { get; set; } = new List<OrderHistory>();
        }
    }
}

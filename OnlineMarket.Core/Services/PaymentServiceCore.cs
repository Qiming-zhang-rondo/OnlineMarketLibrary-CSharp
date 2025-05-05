using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
    public sealed class PaymentServiceCore : IPaymentService
    {
        private readonly ILogger<PaymentServiceCore> logger;
        private readonly AppConfig config;

        private readonly Func<int, IOrderService> getOrderService;
        private readonly Func<int, IShipmentService> getShipmentService;
        private readonly Func<(int sellerId, string productId), IStockService> getStockService;
        private readonly Func<int, ISellerService> getSellerService;
        private readonly Func<int, ICustomerService> getCustomerService;

        private Func<string, string, string, Task> logAction = (_, _, _) => Task.CompletedTask;

        public PaymentServiceCore(
            ILogger<PaymentServiceCore> logger,
            AppConfig config,
            Func<int, IOrderService> getOrderService,
            Func<int, IShipmentService> getShipmentService,
            Func<(int sellerId, string productId), IStockService> getStockService,
            Func<int, ISellerService> getSellerService,
            Func<int, ICustomerService> getCustomerService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.getOrderService = getOrderService ?? throw new ArgumentNullException(nameof(getOrderService));
            this.getShipmentService = getShipmentService ?? throw new ArgumentNullException(nameof(getShipmentService));
            this.getStockService = getStockService ?? throw new ArgumentNullException(nameof(getStockService));
            this.getSellerService = getSellerService ?? throw new ArgumentNullException(nameof(getSellerService));
            this.getCustomerService = getCustomerService ?? throw new ArgumentNullException(nameof(getCustomerService));
        }

        public void SetLogger(Func<string, string, string, Task> logAction)
        {
            this.logAction = logAction ?? ((_, _, _) => Task.CompletedTask);
        }

        public async Task ProcessPayment(InvoiceIssued invoiceIssued)
        {
            int customerId = invoiceIssued.customer.CustomerId;
            int orderId = invoiceIssued.orderId;
            int seq = 1;

            bool isCreditCard = invoiceIssued.customer.PaymentType.Equals(PaymentType.CREDIT_CARD.ToString());
            bool isDebitCard = invoiceIssued.customer.PaymentType.Equals(PaymentType.DEBIT_CARD.ToString());
            bool isBoleto = invoiceIssued.customer.PaymentType.Equals(PaymentType.BOLETO.ToString());

            var orderPayments = new List<OrderPayment>();
            OrderPaymentCard ? card = null;

            if (isCreditCard || isDebitCard)
            {
                var paymentLine = new OrderPayment
                {
                    order_id = orderId,
                    payment_sequential = seq,
                    type = isCreditCard ? PaymentType.CREDIT_CARD : PaymentType.DEBIT_CARD,
                    installments = invoiceIssued.customer.Installments,
                    value = invoiceIssued.totalInvoice,
                    status = Common.Integration.PaymentStatus.succeeded
                };
                orderPayments.Add(paymentLine);

                card = new OrderPaymentCard
                {
                    order_id = orderId,
                    payment_sequential = seq,
                    card_number = invoiceIssued.customer.CardNumber,
                    card_holder_name = invoiceIssued.customer.CardHolderName,
                    card_expiration = invoiceIssued.customer.CardExpiration,
                    card_brand = invoiceIssued.customer.CardBrand
                };

                seq++;
            }

            if (isBoleto)
            {
                orderPayments.Add(new OrderPayment
                {
                    order_id = orderId,
                    payment_sequential = seq,
                    type = PaymentType.BOLETO,
                    installments = 1,
                    value = invoiceIssued.totalInvoice,
                    status = Common.Integration.PaymentStatus.succeeded
                });
                seq++;
            }

            foreach (var item in invoiceIssued.items)
            {
                if (item.voucher > 0)
                {
                    orderPayments.Add(new OrderPayment
                    {
                        order_id = orderId,
                        payment_sequential = seq,
                        type = PaymentType.VOUCHER,
                        installments = 1,
                        value = item.voucher
                    });
                    seq++;
                }
            }

            var tasks = new List<Task>();

            // Step 1: Log payment
            if (config.LogRecords)
            {
                var key = new StringBuilder(customerId.ToString())
                    .Append('-')
                    .Append(orderId)
                    .ToString();

                var paymentState = new PaymentState
                {
                    orderPayments = orderPayments,
                    card = card
                };

                var serialized = JsonSerializer.Serialize(paymentState);
                tasks.Add(logAction(nameof(PaymentServiceCore), key, serialized));
            }

            // Step 2: Confirm stock reservation
            foreach (var item in invoiceIssued.items)
            {
                var stockService = getStockService((item.seller_id, item.product_id.ToString()));
                tasks.Add(stockService.ConfirmReservation(item.quantity));
            }

            // Step 3: Notify sellers
            var paymentTs = DateTime.UtcNow;
            var paymentConfirmedWithItems = new PaymentConfirmed(
                invoiceIssued.customer,
                orderId,
                invoiceIssued.totalInvoice,
                invoiceIssued.items,
                paymentTs,
                invoiceIssued.instanceId
            );

            var sellerIds = invoiceIssued.items.Select(x => x.seller_id).ToHashSet();
            foreach (var sellerId in sellerIds)
            {
                var sellerService = getSellerService(sellerId);
                tasks.Add(sellerService.ProcessPaymentConfirmed(paymentConfirmedWithItems));
            }

            // Step 4: Notify customer and order
            var paymentConfirmedNoItems = new PaymentConfirmed(
                invoiceIssued.customer,
                orderId,
                invoiceIssued.totalInvoice,
                null,
                paymentTs,
                invoiceIssued.instanceId
            );

            var customerService = getCustomerService(customerId);
            tasks.Add(customerService.NotifyPaymentConfirmed(paymentConfirmedNoItems));

            var orderService = getOrderService(customerId);
            tasks.Add(orderService.ProcessPaymentConfirmed(paymentConfirmedNoItems));

            await Task.WhenAll(tasks);

            // Step 5: Notify shipment
            var shipmentGroupId = Helper.GetShipmentGroupId(customerId, config.NumShipmentActors);
            var shipmentService = getShipmentService(shipmentGroupId);
            await shipmentService.ProcessShipment(paymentConfirmedWithItems);
        }

        private sealed class PaymentState
        {
            public List<OrderPayment> ? orderPayments { get; set; }
            public OrderPaymentCard? card { get; set; }
        }
    }
}
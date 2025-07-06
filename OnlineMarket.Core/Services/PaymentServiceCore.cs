// OnlineMarket.Core.Services/PaymentServiceCore.cs
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Ports;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Interfaces;

namespace OnlineMarket.Core.Services
{
    public sealed class PaymentServiceCore : IPaymentService
    {
        private readonly int                 _customerId;
        private readonly IStockGateway       _stock;
        private readonly ISellerNotifier     _sellerNtfy;
        private readonly IOrderNotifier      _orderNtfy;
        private readonly ICustomerNotifier   _custNtfy;
        private readonly IShipmentGateway    _ship;
        private readonly IClock              _clock;
        private readonly ILogger             _log;

        public PaymentServiceCore(
            int                customerId,
            IStockGateway      stock,
            ISellerNotifier    sellerNtfy,
            IOrderNotifier     orderNtfy,
            ICustomerNotifier  custNtfy,
            IShipmentGateway   ship,
            IClock             clock,
            ILogger            log)
        {
            _customerId  = customerId;
            _stock       = stock;
            _sellerNtfy  = sellerNtfy;
            _orderNtfy   = orderNtfy;
            _custNtfy    = custNtfy;
            _ship        = ship;
            _clock       = clock;
            _log         = log;
        }

        /*──────── IPaymentService ────────*/

        public async Task ProcessPaymentAsync(InvoiceIssued inv)
        {
            // ① Fund flow (only Domain object is generated here, not stored in the database)
            var payTime = _clock.UtcNow;
            var evtWithItems = new PaymentConfirmed(
                    inv.customer, inv.orderId, inv.totalInvoice,
                    inv.items, payTime, inv.instanceId);

            // ② Update inventory: parallel execution
            var stockTasks = inv.items.Select(i =>
                    _stock.ConfirmAsync(i.seller_id, i.product_id, i.quantity));
            await Task.WhenAll(stockTasks);

            // ③ Publish events to each participant
            var tasks = new List<Task>
            {
                _sellerNtfy.NotifyInvoiceAsync(inv),
                _sellerNtfy.NotifyPaymentConfirmedAsync(evtWithItems),
                _orderNtfy .NotifyPaymentAsync(evtWithItems),
                _custNtfy  .NotifyPaymentAsync(evtWithItems)
            };

            await Task.WhenAll(tasks);

            // ④ Trigger the shipping process
            // (using the same event, Shipment will select sellerId to determine the route)
            await _ship.StartShipmentAsync(evtWithItems);
        }
    }
}

// OnlineMarket.Core.Services/OrderServiceCore.cs

using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Interfaces;

namespace OnlineMarket.Core.Services;

public sealed class OrderServiceCore : IOrderService
{
    private readonly int _customerId;
    private readonly IOrderRepository _repo;
    private readonly IStockReserver    _stock;
    private readonly ISellerNotifier  _sellerNtfy;
    private readonly IPaymentGateway  _payment;
    private readonly IClock           _clock;
    private readonly ILogger          _log;

    public OrderServiceCore(int customerId,
                            IOrderRepository repo,
                            IStockReserver stock,
                            ISellerNotifier sellerNtfy,
                            IPaymentGateway payment,
                            IClock clock,
                            ILogger log)
    {
        _customerId = customerId;
        _repo       = repo;   _stock = stock;
        _sellerNtfy = sellerNtfy; _payment = payment;
        _clock      = clock;  _log = log;
    }

    /*──────── IOrderService 实现 ────────*/

    public async Task Checkout(ReserveStock rs)
    {
        var now = _clock.UtcNow;

        /* 1) 同步库存网关 ─────*/
        var inStock = new List<CartItem>();
        foreach (var c in rs.items)
            if (await _stock.TryReserveAsync(c) == ItemStatus.IN_STOCK)
                inStock.Add(c);

        /* 2) 生成 Order / Items / History */
        int id   = await _repo.GetNextIdAsync();
        var num  = $"{_customerId}-{now:yyyyMMdd}-{id}";
        BuildOrder(id, num, now, inStock,
                   out var order, out var items, out var hist);
        order.customer_id = _customerId;   

        await _repo.SaveAsync(id, order, items, hist);

        /* 3) 通知卖家 + 支付 */
        var invoice = new InvoiceIssued(rs.customerCheckout, id, num,
                                        now, order.total_invoice, items,
                                        rs.instanceId);
        await _sellerNtfy.NotifyInvoiceAsync(invoice);
        //已经调用ProcessPayment(invoice)
        await _payment.StartPaymentAsync(invoice);
    }

    public async Task ProcessPaymentConfirmed(PaymentConfirmed e)
        => await UpdateStatus(e.orderId, OrderStatus.PAYMENT_PROCESSED);

    public async Task ProcessPaymentFailed(PaymentFailed e)
        => await UpdateStatus(e.orderId, OrderStatus.PAYMENT_FAILED, close:true);

    public async Task ProcessShipmentNotification(ShipmentNotification sn)
    {
        var st = sn.Status switch
        {
            ShipmentStatus.approved             => OrderStatus.READY_FOR_SHIPMENT,
            ShipmentStatus.delivery_in_progress => OrderStatus.IN_TRANSIT,
            ShipmentStatus.concluded            => OrderStatus.DELIVERED,
            _                                   => OrderStatus.INVOICED
        };
        await UpdateStatus(sn.OrderId, st,
                           close: st == OrderStatus.DELIVERED,
                           deliveredDate: sn.EventDate);
    }

    public Task<List<Order>> GetOrders()   => _repo.QueryByCustomerAsync(_customerId);
    public Task<int>        GetNumOrders() => _repo.CountAsync();
    public Task Reset()                    => _repo.ResetAsync(_customerId);

    /*──────── 私有辅助 ────────*/
    private static void BuildOrder(int id, string invoiceNum, DateTime now,
                               IReadOnlyList<CartItem> itemsInStock,
                               out Order order,
                               out List<OrderItem> items,
                               out List<OrderHistory> hist)
{
    /*── 1) 汇总金额 ───────────────────────*/
    float freight = itemsInStock.Sum(i => i.FreightValue);
    float gross   = itemsInStock.Sum(i => i.UnitPrice * i.Quantity);

    float incentive = itemsInStock.Sum(i =>
        Math.Min(i.Voucher, i.UnitPrice * i.Quantity));

    float net       = gross - incentive;
    float totalInv  = net + freight;

    /*── 2) Order 记录 ─────────────────────*/
    order = new Order
    {
        id               = id,
        customer_id      = 0,               // 调用者稍后再填
        invoice_number   = invoiceNum,
        status           = OrderStatus.INVOICED,
        purchase_date    = now,
        total_amount     = net,
        total_items      = gross,
        total_freight    = freight,
        total_incentive  = incentive,
        total_invoice    = totalInv,
        count_items      = itemsInStock.Count,
        created_at       = now,
        updated_at       = now
    };

    /*── 3) OrderItem 列表 ─────────────────*/
    items = new();
    int itemId = 1;
    foreach (var c in itemsInStock)
    {
        float itemGross = c.UnitPrice * c.Quantity;
        float itemInc   = Math.Min(c.Voucher, itemGross);
        float itemNet   = itemGross - itemInc;

        items.Add(new OrderItem
        {
            order_id       = id,
            order_item_id  = itemId++,
            product_id     = c.ProductId,
            product_name   = c.ProductName,
            seller_id      = c.SellerId,
            unit_price     = c.UnitPrice,
            quantity       = c.Quantity,
            total_items    = itemGross,
            total_amount   = itemNet,
            freight_value  = c.FreightValue,
            shipping_limit_date = now.AddDays(3),
            voucher        = c.Voucher
        });
    }

    /*── 4) 初始历史 ───────────────────────*/
    hist = new()
    {
        new OrderHistory
        {
            order_id   = id,
            created_at = now,
            status     = OrderStatus.INVOICED
        }
    };
}

    private async Task UpdateStatus(int id, OrderStatus st,
                                    bool close = false, DateTime? deliveredDate = null)
    {
        var (order, items, hist) = await _repo.LoadAsync(id);
        if (order is null) { _log.LogWarning("Order {Id} not found", id); return; }

        order.status      = st;
        order.updated_at  = _clock.UtcNow;
        if (deliveredDate.HasValue) order.delivered_customer_date = deliveredDate.Value;

        hist.Add(new OrderHistory{ order_id=id, created_at=_clock.UtcNow, status=st });

        if (close) await _repo.DeleteAsync(id);
        else       await _repo.SaveAsync(id, order, items, hist);
    }
}

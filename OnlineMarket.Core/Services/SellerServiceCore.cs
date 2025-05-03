using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace OnlineMarket.Core.Services;


public sealed class SellerServiceCore : ISellerService
{
    private readonly int _sellerId;
    private readonly ISellerRepository _repo;
    private readonly IAuditLog _audit;
    private readonly IClock _clock;
    private readonly ILogger _log;
    private readonly bool _logRecords;

    private Seller? _seller;
    private IDictionary<string,List<OrderEntry>> _entries = 
        new Dictionary<string,List<OrderEntry>>();

    public SellerServiceCore(int sellerId,
                             ISellerRepository repo,
                             IAuditLog audit,
                             IClock clock,
                             ILogger<SellerServiceCore> log,
                             bool logRecords)
    {
        _sellerId   = sellerId;
        _repo       = repo;
        _audit      = audit;
        _clock      = clock;
        _log        = log;
        _logRecords = logRecords;
    }

    /*──────── ISellerService 实现 ────────*/

    public async Task SetSeller(Seller seller)
    {
        _seller = seller;
        await _repo.SaveSellerAsync(seller);
    }

    public Task<Seller?> GetSeller() => Task.FromResult(_seller);
    
    public async Task ProcessNewInvoice(InvoiceIssued invoice)
    {
        var list = invoice.items.Select(item => new OrderEntry
        {
            customer_id    = invoice.customer.CustomerId,
            order_id       = item.order_id,
            natural_key    = $"{invoice.customer.CustomerId}|{item.order_id}",
            seller_id      = item.seller_id,
            product_id     = item.product_id,
            product_name   = item.product_name,
            quantity       = item.quantity,
            total_amount   = item.total_amount,
            total_items    = item.total_items,
            total_invoice  = item.total_amount + item.freight_value,
            total_incentive= item.voucher,
            freight_value  = item.freight_value,
            order_status   = OrderStatus.INVOICED,
            unit_price     = item.unit_price
        }).ToList();

        string key = BuildKey(invoice.customer.CustomerId, invoice.orderId);
        if (!_entries.ContainsKey(key))
            _entries[key] = list;
        

        await _repo.SaveEntriesAsync(_entries);
        
    }

    public async Task ProcessPaymentConfirmed(PaymentConfirmed evt)
    {
        string key = BuildKey(evt.customer.CustomerId, evt.orderId);
        if (_entries.TryGetValue(key, out var list))
        {
            foreach (var e in list) e.order_status = OrderStatus.PAYMENT_PROCESSED;
            await _repo.SaveEntriesAsync(_entries);
        }
    }

    public async Task ProcessPaymentFailed(PaymentFailed evt)
    {
        string key = BuildKey(evt.customer.CustomerId, evt.orderId);
        if (_entries.TryGetValue(key, out var list))
        {
            foreach (var e in list) e.order_status = OrderStatus.PAYMENT_FAILED;
            await _repo.SaveEntriesAsync(_entries);
        }
    }

    public async Task ProcessShipmentNotification(ShipmentNotification sn)
    {
        string key = BuildKey(sn.CustomerId, sn.OrderId);
        if (!_entries.TryGetValue(key, out var list))
        {
            _log.LogDebug("ShipmentNotification ignored – key {Key} not found.", key);
            return;
        }

        if (sn.Status == ShipmentStatus.concluded)
        {
            if (_logRecords)
                await _audit.WriteAsync("SellerActor", key,
                    JsonSerializer.Serialize(list));
            _entries.Remove(key);
        }
        else
        {
            foreach (var e in list)
            {
                if (sn.Status == ShipmentStatus.approved)
                {
                    e.order_status     = OrderStatus.READY_FOR_SHIPMENT;
                    e.shipment_date    = sn.EventDate;
                    e.delivery_status  = PackageStatus.ready_to_ship;
                }
                else if (sn.Status == ShipmentStatus.delivery_in_progress)
                {
                    e.order_status    = OrderStatus.IN_TRANSIT;
                    e.delivery_status = PackageStatus.shipped;
                }
            }
        }
        await _repo.SaveEntriesAsync(_entries);
    }

    public async Task ProcessDeliveryNotification(DeliveryNotification dn)
    {
        string key = BuildKey(dn.customerId, dn.orderId);
        if (!_entries.TryGetValue(key, out var list)) return;

        var entry = list.FirstOrDefault(x => x.product_id == dn.productId);
        if (entry is not null)
        {
            entry.package_id     = dn.packageId;
            entry.delivery_date  = dn.deliveryDate;
            entry.delivery_status= PackageStatus.delivered;
            await _repo.SaveEntriesAsync(_entries);
        }
    }

    public Task<SellerDashboard> QueryDashboard()
    {
        var flat = _entries.SelectMany(x => x.Value).ToList();
        var view = new OrderSellerView
        {
            seller_id     = _sellerId,
            count_orders  = flat.Select(x => x.order_id).Distinct().Count(),
            count_items   = flat.Count,
            total_invoice = flat.Sum(x => x.total_invoice),
            total_amount  = flat.Sum(x => x.total_amount),
            total_freight = flat.Sum(x => x.freight_value),
            total_incentive=flat.Sum(x => x.total_incentive),
            total_items   = flat.Sum(x => x.total_items)
        };
        return Task.FromResult(new SellerDashboard(view, flat));
    }

    public async Task Reset()
    {
        _seller  = null;
        _entries.Clear();
        await _repo.ResetAsync(_sellerId);
    }

    /*───────── 辅助 ─────────*/
    private static string BuildKey(int cid, int oid) => $"{cid}-{oid}";
}

// ──────────────────────────────────────────────────────────────
// OnlineMarket.Core.Services/SellerViewServiceCore.cs
// ──────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;

namespace OnlineMarket.Core.Services;

public sealed class SellerViewServiceCore : ISellerViewService
{
    private readonly int _sellerId;

    // Ports
    private readonly IOrderEntryViewRepository _repo;
    private readonly IMaterializedViewRefresher _viewRefresher;
    private readonly IAuditLog _audit;
    private readonly IClock _clock;
    private readonly ILogger _log;

    private readonly bool _logRecords;

    // State Cache (only valid during the lifecycle of this grain)
    private Seller? _seller;
    /// <summary>key = (customerId, orderId) → entryId 列表</summary>
    private IDictionary<(int,int), List<int>> _cache =
        new Dictionary<(int,int), List<int>>();

    private SellerDashboard _cachedDashboard =
        new(new OrderSellerView(), new List<OrderEntry>());

    private volatile bool _dirty = true;

    /*──────────────────── ctor ────────────────────*/
    public SellerViewServiceCore(
        int sellerId,
        IOrderEntryViewRepository repo,
        IMaterializedViewRefresher refresher,
        IAuditLog audit,
        IClock clock,
        ILogger log,
        bool logRecords)
    {
        _sellerId      = sellerId;
        _repo          = repo  ?? throw new ArgumentNullException(nameof(repo));
        _viewRefresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
        _audit         = audit ?? throw new ArgumentNullException(nameof(audit));
        _clock         = clock ?? throw new ArgumentNullException(nameof(clock));
        _log           = log   ?? throw new ArgumentNullException(nameof(log));
        _logRecords    = logRecords;
    }

    /*──────────────── ISellerViewService ────────────────*/

    public Task SetSeller(Seller seller)
    {
        _seller = seller ?? throw new ArgumentNullException(nameof(seller));
        // Seller 仅保存在 OrleansState（Impl 层负责），此处无需落库
        return Task.CompletedTask;
    }
    public Task<Seller?> GetSeller() => Task.FromResult(_seller);

    /*―――――――― Invoice ――――――――*/
    public async Task ProcessNewInvoice(InvoiceIssued inv)
    {
        
        
        var list = inv.items.Select(i => new OrderEntry
        {
            customer_id  = inv.customer.CustomerId,
            order_id     = i.order_id,
            seller_id    = i.seller_id,

            natural_key  = $"{inv.customer.CustomerId}|{i.order_id}",
            
            product_id   = i.product_id,
            product_name = i.product_name,
            quantity     = i.quantity,
            unit_price   = i.unit_price,
            total_items  = i.total_items,
            total_amount = i.total_amount,
            freight_value= i.freight_value,
            total_invoice= i.total_amount + i.freight_value,
            total_incentive = i.voucher,

            // Initial state
            order_status   = OrderStatus.INVOICED,
            delivery_status= PackageStatus.created
        }).ToList();

        await _repo.AddEntriesAsync(list);

        // Record to cache: for quick location of subsequent shipment/delivery
        _cache[(inv.customer.CustomerId, inv.orderId)] =
            list.Select(e => e.id).ToList();
        // await _repo.SaveCacheAsync(_cache);
        var key = (inv.customer.CustomerId, inv.orderId);
        var ids = list.Select(e => e.id).ToList();

        Console.WriteLine($">>> Cache key={key}, ids={string.Join(",", ids)}");
        
        await _repo.SaveCacheAsync(_cache);
        Console.WriteLine(">>> SaveCacheAsync(repo) returned");

        _dirty = true;                                  // 仪表盘需要刷新
    }

    /*―――――――― Payment result (not relevant in the view)――――――――*/
    public Task ProcessPaymentConfirmed(PaymentConfirmed _) => Task.CompletedTask;
    public Task ProcessPaymentFailed   (PaymentFailed   _)  => Task.CompletedTask;

    /*―――――――― Shipment Status ――――――――*/
    public async Task ProcessShipmentNotification(ShipmentNotification sn)
    {
        var key = (sn.CustomerId, sn.OrderId);
        // Console.WriteLine($">>> Entering ProcessShipmentNotification for key={key}, status={sn.Status}");
        //
        // 
        // Console.WriteLine($">>> Cache contains key? {_cache.ContainsKey(key)}");
        if (!_cache.TryGetValue(key, out var ids))
        {
            Console.WriteLine($">>> Cache miss for {key}, exiting");
            return;
        }
        // Console.WriteLine($">>> Cache hit, ids = [{string.Join(',', ids)}]");

        // 1) Finished: Delete + Audit
        if (sn.Status == ShipmentStatus.concluded)
        {
            Console.WriteLine(">>> Branch: concluded");

            // 删除
            await _repo.DeleteEntriesAsync(ids);
            Console.WriteLine($">>> Deleted {ids.Count} entries");

            if (_logRecords)
            {
                var json = JsonSerializer.Serialize(ids);
                await _audit.WriteAsync("SellerView",
                    $"{key.CustomerId}-{key.OrderId}", json);
            }

            _cache.Remove(key);
            await _repo.SaveCacheAsync(_cache);
        }
        // 2) approved→ READY_FOR_SHIPMENT
        else if (sn.Status == ShipmentStatus.approved)
        {
            Console.WriteLine($">>> Branch: approved");
            var upd = ids.Select(id => new OrderEntry
            {
                id              = id,
                // natural_key     = $"{sn.CustomerId}|{sn.OrderId}",
                order_status    = OrderStatus.READY_FOR_SHIPMENT,
                shipment_date   = sn.EventDate,
                delivery_status = PackageStatus.ready_to_ship
            });
            await _repo.UpdateEntriesAsync(upd,
                e => e.order_status,
                e => e.shipment_date,
                e => e.delivery_status
                );
            Console.WriteLine($">>> Updated entries to ready_to_ship");
        }
        // 3) delivery_in_progress→ IN_TRANSIT
        else if (sn.Status == ShipmentStatus.delivery_in_progress)
        {
            var upd = ids.Select(id => new OrderEntry
            {
                id              = id,
                order_status    = OrderStatus.IN_TRANSIT,
                delivery_status = PackageStatus.shipped
            });
            await _repo.UpdateEntriesAsync(upd, 
                e => e.order_status,
                e => e.delivery_status);
        }

        _dirty = true;
    }

    /*―――――――― Delivery package（This view does not contain details）――――――*/
    public Task ProcessDeliveryNotification(DeliveryNotification _) => Task.CompletedTask;

    /*―――――――― Dashboard query ――――――――*/
    public async Task<SellerDashboard> QueryDashboard()
    {
        if (!_dirty) return _cachedDashboard;
        
        await _viewRefresher.RefreshAsync(_sellerId);
        
        var flat = (await _repo.QueryEntriesBySellerAsync(_sellerId)).ToList();
        
        var view = new OrderSellerView(_sellerId)
        {
            count_orders    = flat.Select(e => e.order_id).Distinct().Count(),
            count_items     = flat.Count,
            total_invoice   = flat.Sum(e => e.total_invoice),
            total_amount    = flat.Sum(e => e.total_amount),
            total_freight   = flat.Sum(e => e.freight_value),
            total_incentive = flat.Sum(e => e.total_incentive),
            total_items     = flat.Sum(e => e.total_items)
        };
        
        _cachedDashboard = new SellerDashboard(view, flat);
        _dirty = false;
        return _cachedDashboard;
    }

    /*―――――――― Reset ――――――――*/
    public async Task Reset()
    {
        _cache.Clear();
        await _repo.ResetAsync(_sellerId);
        _dirty = true;
    }
}

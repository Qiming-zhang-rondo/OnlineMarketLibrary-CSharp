using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;

namespace OnlineMarket.Core.Services;

public sealed class ShipmentServiceCore : IShipmentService
{
    private readonly IShipmentRepository _repo;
    private readonly ISellerNotifier     _sellerNtfy;
    private readonly IOrderNotifier      _orderNtfy;
    private readonly IAuditLog           _audit;
    private readonly IClock              _clock;
    private readonly ILogger<ShipmentServiceCore> _log;

    public ShipmentServiceCore(
        IShipmentRepository repo,
        ISellerNotifier sellerNtfy,
        IOrderNotifier orderNtfy,
        IAuditLog audit,
        IClock clock,
        ILogger<ShipmentServiceCore> log)
    {
        _repo       = repo;
        _sellerNtfy = sellerNtfy;
        _orderNtfy  = orderNtfy;
        _audit      = audit;
        _clock      = clock;
        _log        = log;
    }

    /*──────────── IShipmentService ───────────*/

    public async Task ProcessShipment(PaymentConfirmed pc)
    {
        DateTime now = _clock.UtcNow;

        var shipment = new Shipment
        {
            order_id             = pc.orderId,
            customer_id          = pc.customer.CustomerId,
            package_count        = pc.items.Count,
            total_freight_value  = pc.items.Sum(i => i.freight_value),
            request_date         = now,
            status               = ShipmentStatus.approved,
            first_name = pc.customer.FirstName,
            last_name  = pc.customer.LastName,
            street     = pc.customer.Street,
            complement = pc.customer.Complement,
            zip_code   = pc.customer.ZipCode,
            city       = pc.customer.City,
            state      = pc.customer.State
        };

        int id = await _repo.GetNextIdAsync();
        var packages = BuildPackages(id, pc.customer.CustomerId, pc.items, now);

        await _repo.SaveAsync(id, shipment, packages);

        var sn = new ShipmentNotification(pc.customer.CustomerId, pc.orderId,
                                          now, pc.instanceId, ShipmentStatus.approved, sellerId: 0);
        
        
        //Modify ShipmentNotification according to the original code logic and seller related
        //Notify all sellers
        foreach (var sellerId in packages.Select(p => p.seller_id).Distinct())
            await _sellerNtfy.NotifyShipment(sn with { SellerId = sellerId });

        await _orderNtfy.NotifyShipment(sn);
    }

    public async Task UpdateShipment(string tid)
    {
        var oldest = await _repo.OldestOpenPerSellerAsync(10);
        await DoUpdate(oldest, tid);
    }

    public Task UpdateShipment(string tid, ISet<(int customerId,int orderId,int sellerId)> _) =>
        throw new NotImplementedException();

    public Task<List<Shipment>> GetShipments(int customerId) =>
        _repo.QueryByCustomerAsync(customerId);

    public Task Reset() => _repo.ResetAsync();

    /*──────────── Private ───────────*/
    private List<Package> BuildPackages(int shipmentId, int customerId, IReadOnlyCollection<OrderItem> items, DateTime now)
    {
        int idx = 1;
        return items.Select(i => new Package
        {
            shipment_id   = shipmentId,
            order_id      = i.order_id,
            customer_id   = customerId,
            package_id    = idx++,
            status        = PackageStatus.shipped,
            freight_value = i.freight_value,
            shipping_date = now,
            seller_id     = i.seller_id,
            product_id    = i.product_id,
            product_name  = i.product_name,
            quantity      = i.quantity
        }).ToList();
    }

    private async Task DoUpdate(Dictionary<int,int> oldest, string tid)
    {
        DateTime now = _clock.UtcNow;
        foreach (var (sellerId, shipmentId) in oldest)
        {
            var (ship, pkgs) = await _repo.LoadAsync(shipmentId);

            var sellerPkgs   = pkgs.Where(p => p.seller_id == sellerId).ToList();
            int deliveredCnt = pkgs.Count(p => p.status == PackageStatus.delivered);

            foreach (var pkg in sellerPkgs)
            {
                pkg.status        = PackageStatus.delivered;
                pkg.delivery_date = now;

                var dn = new DeliveryNotification(
                            ship.customer_id, pkg.order_id, pkg.package_id,
                            pkg.seller_id, pkg.product_id, pkg.product_name,
                            PackageStatus.delivered, now, tid);

                await _sellerNtfy.NotifyDelivery(dn);
            }

            if (ship.status == ShipmentStatus.approved)
            {
                ship.status = ShipmentStatus.delivery_in_progress;
                await _orderNtfy.NotifyShipment(
                    new ShipmentNotification(ship.customer_id, ship.order_id,
                                             now, tid, ship.status, sellerId: 0));
            }

            if (ship.package_count == deliveredCnt + sellerPkgs.Count)
            {
                ship.status = ShipmentStatus.concluded;

                await _orderNtfy.NotifyShipment(
                    new ShipmentNotification(ship.customer_id, ship.order_id,
                                             now, tid, ship.status, sellerId: 0));
                
                // await _audit.WriteAsync("ShipmentActor",
                //          $"{ship.customer_id}-{ship.order_id}",
                //          JsonSerializer.Serialize(new { ship, pkgs }));

                await _repo.DeleteAsync(shipmentId);
                continue;     
            }

            await _repo.SaveAsync(shipmentId, ship, pkgs);
        }
    }
}

// ───────────────────────────────────────────────
// OnlineMarket.OrleansImpl.Infra.SellerDb/SellerDbContext.cs
// ───────────────────────────────────────────────
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Common.Config;

namespace OnlineMarket.OrleansImpl.Infra.SellerDb;

public sealed class SellerDbContext : DbContext
{
    /*─────────────── DbSet ───────────────*/
    public DbSet<OrderEntry>     OrderEntries     => Set<OrderEntry>();
    public DbSet<OrderSellerView> OrderSellerView => Set<OrderSellerView>();

    private readonly AppConfig _cfg;

    /*──── Constructor: EF / AddDbContextFactory needs this ────*/
    [ActivatorUtilitiesConstructor]
    public SellerDbContext(DbContextOptions<SellerDbContext> opts,
                           AppConfig cfg) : base(opts)
    {
        _cfg = cfg;
    }

    /*────────────── OnConfiguring ──────────────*/
    // protected override void OnConfiguring(DbContextOptionsBuilder b)
    // {
    //     /* If AddDbContextFactory has already been configured externally, there is no need to configure it repeatedly. */
    //     if (!b.IsConfigured)
    //     {
    //         b.UseNpgsql(_cfg.AdoNetConnectionString)
    //          .EnableSensitiveDataLogging()
    //          .EnableDetailedErrors()
    //          .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    //     }
    // }

    /*────────────── OnModelCreating ──────────────*/
    protected override void OnModelCreating(ModelBuilder m)
    {
        m.HasDefaultSchema("public");

        /*—— OrderEntry ——*/
        m.Entity<OrderEntry>(e =>
        {
            e.ToTable("order_entries");
            e.HasKey(oe => oe.id);
            e.Property(p => p.id).ValueGeneratedOnAdd();
            e.HasIndex(p => p.seller_id, "seller_idx");
            e.Property(p => p.order_status   ).HasConversion<string>();
            e.Property(p => p.delivery_status).HasConversion<string>();
            e.Property(x => x.product_category)
                .HasColumnName("product_category")
                .HasColumnType("text")
                .IsRequired(false);  // ← 允许为 NULL
        });

        /*—— Materialized views (only mapped, EF is not responsible for building) ——*/
        m.Entity<OrderSellerView>(e =>
        {
            e.HasNoKey();
            e.ToView("order_seller_view");   // 通用聚合视图
        });
    }

    /*────────────── SQL ──────────────*/

    // Create/rebuild a materialized view of a single seller - one SQL
    public static string CreateSellerViewSql(int sellerId) => $@"
create materialized view if not exists public.order_seller_view_{sellerId} as
select
  seller_id,
  count(distinct natural_key)           as count_orders,
  count(product_id)                     as count_items,
  sum(total_amount)                     as total_amount,
  sum(freight_value)                    as total_freight,
  sum(total_items - total_amount)       as total_incentive,
  sum(total_invoice)                    as total_invoice,
  sum(total_items)                      as total_items
from public.order_entries
where seller_id = {sellerId}
group by seller_id;";

    public static string DropSellerViewSql(int sellerId) =>
        $"drop materialized view if exists public.order_seller_view_{sellerId};";

    public static string RefreshSellerViewSql(int sellerId) =>
        $"refresh materialized view public.order_seller_view_{sellerId}";
}

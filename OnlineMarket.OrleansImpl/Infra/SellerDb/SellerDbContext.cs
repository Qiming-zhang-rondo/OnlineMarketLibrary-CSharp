﻿namespace OnlineMarket.OrleansImpl.Infra.SellerDb;

using Microsoft.EntityFrameworkCore;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Integration;
using OnlineMarket.Core.Common.Config;


    public sealed class SellerDbContext : DbContext
    {

        public DbSet<OrderEntry> OrderEntries => Set<OrderEntry>();

        public DbSet<OrderSellerView> OrderSellerView => Set<OrderSellerView>();

        private readonly AppConfig appConfig;

        public SellerDbContext(AppConfig appConfig)
        {
            this.appConfig = appConfig;
        }

        //Npgsql版本太低
        // public static string CreateCustomOrderSellerViewSql(int sellerId)
        // {
        //     return $"CREATE MATERIALIZED VIEW IF NOT EXISTS public.order_seller_view_{sellerId} " +
        //                                                     $"AS SELECT seller_id, COUNT(DISTINCT natural_key) as count_orders, COUNT(product_id) as count_items, SUM(total_amount) as total_amount, SUM(freight_value) as total_freight, " +
        //                                                     $"SUM(total_items - total_amount) as total_incentive, SUM(total_invoice) as total_invoice, SUM(total_items) as total_items FROM public.order_entries WHERE seller_id = {sellerId} GROUP BY seller_id";
        // }
        
        public static string CreateCustomOrderSellerViewSql(int sellerId)
        {
            return $@"
                DROP MATERIALIZED VIEW IF EXISTS public.order_seller_view_{sellerId};
                CREATE MATERIALIZED VIEW public.order_seller_view_{sellerId} AS
                  SELECT
                    seller_id,
                    COUNT(DISTINCT natural_key)       AS count_orders,
                    COUNT(product_id)                 AS count_items,
                    SUM(total_amount)                 AS total_amount,
                    SUM(freight_value)                AS total_freight,
                    SUM(total_items - total_amount)   AS total_incentive,
                    SUM(total_invoice)                AS total_invoice,
                    SUM(total_items)                  AS total_items
                  FROM public.order_entries
                  WHERE seller_id = {sellerId}
                  GROUP BY seller_id;
                ";
        }

        public static string GetRefreshCustomOrderSellerViewSql(int sellerId)
        {
            return $"REFRESH MATERIALIZED VIEW public.order_seller_view_{sellerId};";
        }

         protected override void OnConfiguring(DbContextOptionsBuilder options)
         {
             options
                 .UseNpgsql(appConfig.AdoNetConnectionString)
                 .EnableSensitiveDataLogging()
                 .EnableDetailedErrors()
                 .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
         }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<OrderEntry>().ToTable("order_entries");

            modelBuilder.Entity<OrderEntry>(e =>
            {
                e.HasKey(oe => oe.id);
                e.Property(e => e.id).ValueGeneratedOnAdd();

                // to accelerate refresh view for each seller
                e.HasIndex(oe =>  oe.seller_id, "seller_idx");

                e.Property(e => e.order_status).HasConversion<string>();
                e.Property(e => e.delivery_status).HasConversion<string>();
            });

            modelBuilder.Entity<OrderSellerView>(e =>
            {
                e.HasNoKey();
                e.ToView("order_seller_view");
            });

        }
    }
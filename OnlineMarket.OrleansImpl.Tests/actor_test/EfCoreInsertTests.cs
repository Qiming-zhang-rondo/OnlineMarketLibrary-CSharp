using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.OrleansImpl.Infra.SellerDb;
using Xunit;

public class EfCoreInsertTests
{
    private readonly IServiceProvider _services;

    public EfCoreInsertTests()
    {
        // Here we construct a ServiceProvider directly, just for illustration.
        // In your actual project, you may get it from TestCluster.ServiceProvider.
        var cfg = new AppConfig { AdoNetConnectionString = "Host=localhost;Port=5432;Database=online_test;Username=online;Password=online" };

        var services = new ServiceCollection();
        services.AddSingleton(cfg);
        services.AddDbContextFactory<SellerDbContext>(opts =>
            opts.UseNpgsql(cfg.AdoNetConnectionString)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        );
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task DirectAddAndSave_InsertsOrderEntry()
    {
        // Arrange: Create and rebuild the database
        var factory = _services.GetRequiredService<IDbContextFactory<SellerDbContext>>();
        await using var ctx = await factory.CreateDbContextAsync();
        ctx.Database.EnsureDeleted();
        ctx.Database.EnsureCreated();

        var entry = new OrderEntry
        {
            seller_id        = 7001,
            order_id         = 9101,
            customer_id      = 20,
            natural_key      = "7001_9101",
            product_id       = 123,
            product_name     = "TestProduct",
            product_category = "TestCategory",
            quantity         = 2,
            unit_price       = 50.0f,
            total_amount     = 100.0f,
            freight_value    = 5.0f,
            total_items      = 100.0f,
            total_invoice    = 100.0f,
            total_incentive  = 0f,
            order_status     = OrderStatus.INVOICED,
            delivery_status  = PackageStatus.created
        };

        // Act: Add and save directly
        await ctx.OrderEntries.AddAsync(entry);
        await ctx.SaveChangesAsync();

        // Assert: Confirm that there is only this record in the database
        var all = await ctx.OrderEntries
            .Where(e => e.seller_id == 7001 && e.order_id == 9101)
            .ToListAsync();
        
        Assert.Single(all);
        var saved = all[0];
        Assert.Equal(OrderStatus.INVOICED, saved.order_status);
        Assert.Equal(2, saved.quantity);
        Assert.Equal(100.0f, saved.total_amount);
    }
}

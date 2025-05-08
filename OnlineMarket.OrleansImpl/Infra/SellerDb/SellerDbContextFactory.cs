using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using OnlineMarket.Core.Common.Config;

namespace OnlineMarket.OrleansImpl.Infra.SellerDb;

public class SellerDbContextFactory
    : IDesignTimeDbContextFactory<SellerDbContext>
{
    public SellerDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var appCfg = config.GetSection("AppConfig").Get<AppConfig>();
        var builder = new DbContextOptionsBuilder<SellerDbContext>();
        builder.UseNpgsql(config.GetConnectionString("Pg"))
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        return new SellerDbContext(builder.Options, appCfg);
    }
}
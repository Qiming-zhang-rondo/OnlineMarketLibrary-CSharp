// Tests/Infra/SellerViewTestSiloConfigurator.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Orleans.Hosting;
using OnlineMarket.OrleansImpl.Infra.SellerDb;
using Orleans.TestingHost;

public sealed class SellerViewTestSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.ConfigureServices(services =>
        {
            // 用 EF Core In-Memory Provider，避免真实数据库依赖
            services.AddDbContext<SellerDbContext>(opt =>
                opt.UseInMemoryDatabase("SellerViewTests"));
        });
    }
}
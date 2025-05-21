using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CartMS; 

namespace OnlineMarket.DaprImpl.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            // 可以加载测试用的 appsettings.json
            configBuilder.AddJsonFile("appsettings.Test.json", optional: true);
        });

        builder.ConfigureServices(services =>
        {
            // 如果有需要 mock 的 service，可在这里替换
        });

        return base.CreateHost(builder);
    }
}
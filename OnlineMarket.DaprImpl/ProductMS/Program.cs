// Program.cs

using Dapr.Client;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Services;
using ProductMS.Gateways;
using ProductMS.Infractructure;
using ProductMS.Infrastructure;
using ProductMS.Services;

namespace ProductMS;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 添加 Dapr 客户端
        builder.Services.AddDaprClient();

        // Core & Infrastructure 注册
        builder.Services.AddSingleton<IClock, SystemClock>();
        builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        builder.Services.AddSingleton<IReplicationPublisher, DaprReplicationPublisher>();
        builder.Services.AddSingleton<IStockNotifier, DaprStockNotifier>();

        // 注入 ProductServiceCore + 封装的 DaprProductService
        builder.Services.AddScoped<IProductService>(sp =>
        {
            var repo = sp.GetRequiredService<IProductRepository>();
            var replicator = sp.GetRequiredService<IReplicationPublisher>();
            var stock = sp.GetRequiredService<IStockNotifier>();
            var clock = sp.GetRequiredService<IClock>();
            var loggerCore = sp.GetRequiredService<ILogger<ProductServiceCore>>();

            var core = new ProductServiceCore(
                sellerId: 1,
                productId: 1,
                repo,
                replicator,
                stock,
                clock,
                loggerCore,
                enableStreamReplication: true, // 你看是否默认开
                enableSnapshot: false
            );

            var daprClient = sp.GetRequiredService<DaprClient>();
            var loggerDapr = sp.GetRequiredService<ILogger<DaprProductService>>();

            return new DaprProductService(daprClient, core, loggerDapr);
        });

        builder.Services.AddControllers().AddDapr();  // 让 [Topic] 生效
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseCloudEvents();
        app.MapControllers();
        app.MapSubscribeHandler(); // 订阅处理

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.Run();
    }
}
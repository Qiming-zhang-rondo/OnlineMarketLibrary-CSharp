// Program.cs
using CartMS.Infrastructure;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Services;
using OnlineMarket.Core.Ports;
using Dapr.Client;
using CartMS.Repository;
using CartMS.Gateways;
using CartMS.Services;  // 👈 别忘了引入 Adapter

namespace CartMS;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ---- Dapr ---------------------------------
        builder.Services.AddDaprClient();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        // ---- Core & Infrastructure ---------------
        builder.Services.AddSingleton<IClock, SystemClock>();
        builder.Services.AddSingleton<ICartRepository, InMemoryCartRepository>();
        builder.Services.AddSingleton<IOrderGateway, DaprOrderGateway>();

        // 注册 CartServiceCore（Core Service）
        builder.Services.AddScoped<CartServiceCore>(sp =>
        {
            var repo = sp.GetRequiredService<ICartRepository>();
            var gw = sp.GetRequiredService<IOrderGateway>();
            var clk = sp.GetRequiredService<IClock>();
            var logger = sp.GetRequiredService<ILogger<CartServiceCore>>();
            int cid = 1;  // 演示写死 1
            return new CartServiceCore(cid, repo, gw, clk, logger, trackHistory: false);
        });

        // 注册 Adapter：CartMS.Services.CartService
        builder.Services.AddScoped<CartMS.Services.CartService>(sp =>
        {
            var core = sp.GetRequiredService<CartServiceCore>();
            var dapr = sp.GetRequiredService<DaprClient>();
            var logger = sp.GetRequiredService<ILogger<CartMS.Services.CartService>>();
            return new CartMS.Services.CartService(core, dapr, logger);
        });

        // ICartService 绑定到 Adapter（方便 Controller 无感知获取）
        builder.Services.AddScoped<ICartService>(sp =>
            sp.GetRequiredService<CartMS.Services.CartService>());

        // 注册控制器等
        builder.Services.AddControllers().AddDapr();  // 让 [Topic] 生效
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();
        app.UseCloudEvents();
        app.MapControllers();
        app.MapSubscribeHandler();    // Dapr 订阅

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // 全局异常处理
        app.UseExceptionHandler(a => a.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(exception, "Unhandled exception!");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = exception?.Message });
        }));

        app.Run();
    }
}
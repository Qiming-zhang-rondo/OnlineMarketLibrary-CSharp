using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Infra.SellerDb;
using Orleans.Serialization;
using Orleans.TestingHost;

namespace OnlineMarket.OrleansImpl.Tests.Infra.Eventual;

// ───────────────────────────────────────────────
// 1) 先把连接串存到一个 static 供 configurator 读取
// ───────────────────────────────────────────────
internal static class PgTestSettings
{
    public static readonly IConfigurationRoot CfgRoot =
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)   // ★ 关键
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();

    public static readonly string Conn = CfgRoot.GetConnectionString("Pg")!;
    public static readonly AppConfig AppCfg =
        CfgRoot.GetSection("AppConfig").Get<AppConfig>()!;
    
    // 确保 AppConfig 里也有连接串（SellerDbContext.OnConfiguring 会用到）
    static PgTestSettings()
    {
        if (string.IsNullOrWhiteSpace(AppCfg.AdoNetConnectionString))
            AppCfg.AdoNetConnectionString = Conn;
    }
}

// ───────────────────────────────────────────────
// 2) Silo 端：注入 DbContextFactory <SellerDbContext>
// ───────────────────────────────────────────────
public sealed class PgSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder silo)
    {
        
        silo.ConfigureServices(services =>
        {
            /* ① AppConfig 注入 ——★  */
            services.AddSingleton(PgTestSettings.AppCfg);
            
            /* ② DbContextFactory -> Npgsql */
            services.AddDbContextFactory<SellerDbContext>(opt =>
                opt.UseNpgsql(PgTestSettings.Conn));

            services.AddSerializer(sb =>
            {
                sb.AddNewtonsoftJsonSerializer(isSupported: type => 
                    type.Namespace.StartsWith("OnlineMarket.Core.Common") || 
                    type.Namespace.StartsWith("OnlineMarket.OrleansImpl"));
            });
            
            /* ★★ 关键：给 PersistentState 用的 provider 起名 "OrleansStorage" ★★ */
            silo.AddMemoryGrainStorage(Constants.OrleansStorage); // 或直接写 "OrleansStorage"
            
            if (ConfigHelper.NonTransactionalDefaultAppConfig.LogRecords)
                silo.Services.AddSingleton<IAuditLogger, PostgresAuditLogger>();
            else
                silo.Services.AddSingleton<IAuditLogger, EtcNullPersistence>();
        });
    }
}

// ───────────────────────────────────────────────
// 3) Client 端同理（测试代码里要用到 Factory）
// ───────────────────────────────────────────────
public sealed class PgClientConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration cfg, IClientBuilder client)
    {
        client.ConfigureServices(services =>
        {
            services.AddSingleton(PgTestSettings.AppCfg);      // ★ 同样注入

            services.AddDbContextFactory<SellerDbContext>(opt =>
                opt.UseNpgsql(PgTestSettings.Conn));
                    // .UseSnakeCaseNamingConvention());
            
            services.AddSerializer(sb =>
            {
                sb.AddNewtonsoftJsonSerializer(isSupported: type => 
                    type.Namespace.StartsWith("OnlineMarket.Core.Common") || 
                    type.Namespace.StartsWith("OnlineMarket.OrleansImpl"));
            });
            
            // services.AddMemoryGrainStorage(Constants.OrleansStorage);
            
            if (ConfigHelper.NonTransactionalDefaultAppConfig.LogRecords)
                services.AddSingleton<IAuditLogger, PostgresAuditLogger>();
            else
                services.AddSingleton<IAuditLogger, EtcNullPersistence>();
            
        });
    }
}


public class NonTransactionalClusterFixtureWithDb : IDisposable
{
    public TestCluster Cluster { get; }

    /* ------------ 公共：先把配置文件读进来 ------------ */
    private readonly IConfiguration _cfgRoot;
    private readonly string _pgConn;

    public NonTransactionalClusterFixtureWithDb()
    {
        var cfgRoot = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)  // ← 关键信息
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();

        _pgConn  = cfgRoot.GetConnectionString("Pg")!;  

        /* 把 AppConfig 放进全局 Helper，供 Orleans Grains 使用 */
        ConfigHelper.NonTransactionalDefaultAppConfig =
            cfgRoot.GetSection("AppConfig").Get<AppConfig>();

        var builder = new TestClusterBuilder(1);

        /* --- Silo --- */
        /* --- Client --- */
        builder.AddSiloBuilderConfigurator<PgSiloConfigurator>();
        builder.AddClientBuilderConfigurator<PgClientConfigurator>();
        
        
        
        /* 其余：序列化 / MemoryStorage / AuditLogger … 保持你原来的注册 */

        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose() => Cluster.StopAllSilos();
}


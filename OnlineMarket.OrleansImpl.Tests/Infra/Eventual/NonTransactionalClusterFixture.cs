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

public class NonTransactionalClusterFixture : IDisposable
{
    public TestCluster Cluster { get; private set; }

    private class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder hostBuilder)
        {
            hostBuilder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
            
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional:false);
            var conf = builder.Build();
            var cfg = conf.Get< AppConfig >();  
            
            hostBuilder.Services
                .AddDbContextFactory<SellerDbContext>(opt =>
                    opt.UseNpgsql(cfg.AdoNetConnectionString)
                        .UseSnakeCaseNamingConvention());   // Postgres 常用下划线风格
            
            
            // hostBuilder.Services.AddDbContextFactory<SellerDbContext>(opt =>
            //     opt.UseInMemoryDatabase("TestSellerView"));

            if (ConfigHelper.NonTransactionalDefaultAppConfig.StreamReplication)
            {
                hostBuilder.AddMemoryStreams(Constants.DefaultStreamProvider)
                            .AddMemoryGrainStorage(Constants.DefaultStreamStorage);
            }

            hostBuilder.Services.AddSerializer(ser => { ser.AddNewtonsoftJsonSerializer(isSupported: type => 
                    type.Namespace.StartsWith("OnlineMarket.Core.Common") || 
                    type.Namespace.StartsWith("OnlineMarket.OrleansImpl") ||
                    type.Namespace.StartsWith("Npgsql")); })
             .AddSingleton(ConfigHelper.NonTransactionalDefaultAppConfig);

            // the non transactional grains need grain storage for persistent state on constructor
            hostBuilder.AddMemoryGrainStorage(Constants.OrleansStorage);
            // hostBuilder.Services.AddSingleton<IOrderService, FakeOrderService>();

            if (ConfigHelper.NonTransactionalDefaultAppConfig.LogRecords)
                hostBuilder.Services.AddSingleton<IAuditLogger, PostgresAuditLogger>();
            else
                hostBuilder.Services.AddSingleton<IAuditLogger, EtcNullPersistence>();

        }
    }

    private class ClientConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder
                .Services.AddSerializer(ser =>
                 {
                     ser.AddNewtonsoftJsonSerializer(isSupported: type => 
                         type.Namespace.StartsWith("OnlineMarket.Core.Common") || 
                         type.Namespace.StartsWith("OnlineMarket.OrleansImpl") || 
                         type.Namespace.StartsWith("Npgsql"));
                 })
                .AddSingleton(ConfigHelper.NonTransactionalDefaultAppConfig);

            if (ConfigHelper.NonTransactionalDefaultAppConfig.LogRecords)
                clientBuilder.Services.AddSingleton<IAuditLogger, PostgresAuditLogger>();
            else
                clientBuilder.Services.AddSingleton<IAuditLogger, EtcNullPersistence>();
            // clientBuilder.Services.AddSingleton<IOrderService, FakeOrderService>();
        }
    }

    public NonTransactionalClusterFixture()
    {
        var cfg = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build()
            .Get<AppConfig>();

        ConfigHelper.NonTransactionalDefaultAppConfig = cfg;
        
        var builder = new TestClusterBuilder(1);
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        builder.AddClientBuilderConfigurator<ClientConfigurator>();
        builder.AddSiloBuilderConfigurator<SellerViewTestSiloConfigurator>(); // ★ 加这一行
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

}


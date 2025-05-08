using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            
            hostBuilder.Services.AddDbContextFactory<SellerDbContext>(opt =>
                opt.UseInMemoryDatabase("seller_view_test"));

            if (ConfigHelper.NonTransactionalDefaultAppConfig.StreamReplication)
            {
                hostBuilder.AddMemoryStreams(Constants.DefaultStreamProvider)
                            .AddMemoryGrainStorage(Constants.DefaultStreamStorage);
            }

            hostBuilder.Services.AddSerializer(ser => { ser.AddNewtonsoftJsonSerializer(isSupported: type => 
                    type.Namespace.StartsWith("OnlineMarket.Core.Common") || 
                    type.Namespace.StartsWith("OnlineMarket.OrleansImpl")); })
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
        var builder = new TestClusterBuilder(1);
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        builder.AddClientBuilderConfigurator<ClientConfigurator>();
        //in-memory
        builder.AddSiloBuilderConfigurator<SellerViewTestSiloConfigurator>(); // ★ 加这一行
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

}


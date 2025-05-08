// Tests/Infra/Transactional/TransactionalClusterFixture.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Serialization;
using Orleans.TestingHost;
using OnlineMarket.OrleansImpl.Infra;
// using Orleans.Transactions.Hosting;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.Core.Interfaces;
using Orleans.Hosting;
using Orleans.Transactions;


namespace OnlineMarket.OrleansImpl.Tests.Infra.Transactional;

/// <summary>带 Orleans‑Transactions 的 1 节点测试集群</summary>
public sealed class TransactionalClusterFixture : IDisposable
{
    public TestCluster Cluster { get; }

    /*────────── 内存事务存储 + 日志──────────*/
    private sealed class SiloCfg : ISiloConfigurator
    {
        public void Configure(ISiloBuilder silo)
        {
            silo.ConfigureServices(services =>
            {
                services.AddLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.AddConsole();
                    lb.SetMinimumLevel(LogLevel.Warning);
                });
            });
            
            /* Orleans 内存事务：状态 + 事务日志 */
            silo.UseTransactions();               // 必须！启用事务功能
            

            /* 其他通用注册 —— OrleansStorage / Json 序列化 */
            silo.AddMemoryGrainStorage(Constants.OrleansStorage);

            silo.Services.AddSerializer(cfg =>
                cfg.AddNewtonsoftJsonSerializer(t =>
                    t.Namespace?.StartsWith("OnlineMarket") == true));

            /* AuditLog – 这里用 Null 实现即可 */
            silo.Services.AddSingleton<IAuditLogger, EtcNullPersistence>();
        }
    }

    private sealed class ClientCfg : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration _, IClientBuilder client)
        {
            client.ConfigureServices(services =>
            {
                services.AddLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.AddConsole();
                    lb.SetMinimumLevel(LogLevel.Warning);
                });
            });

            client.Services.AddSerializer(cfg =>
                cfg.AddNewtonsoftJsonSerializer(t =>
                    t.Namespace?.StartsWith("OnlineMarket") == true));
        }
    }

    public TransactionalClusterFixture()
    {
        var builder = new TestClusterBuilder(1);
        builder.AddSiloBuilderConfigurator<SiloCfg>();
        builder.AddClientBuilderConfigurator<ClientCfg>();

        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose() => Cluster.StopAllSilos();
}

/* xUnit Collection tag */
[CollectionDefinition(Name)]
public class TransactionalClusterCollection
    : ICollectionFixture<TransactionalClusterFixture>
{
    public const string Name = "txn‑cluster";
}

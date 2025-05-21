﻿// using Orleans.Configuration;
// using OnlineMarket.OrleansImpl.Infra;
// using Orleans.Serialization;
// // using OnlineMarket.OrleansImpl.Infra.SellerDb;
// using OnlineMarket.OrleansImpl.Infra.Redis;
// // using Microsoft.EntityFrameworkCore;
// using OnlineMarket.Core.Common.Config;
// // using OnlineMarket.OrleansImpl.Service;

// var builder = WebApplication.CreateBuilder(args);
// builder.WebHost.UseUrls("http://localhost:8081");

// IConfigurationSection configSection = builder.Configuration.GetSection("AppConfig");

// var orleansTransactions = configSection.GetValue<bool>("OrleansTransactions");
// var sellerViewPostgres = configSection.GetValue<bool>("SellerViewPostgres");
// var shipmentUpdatePostgres = configSection.GetValue<bool>("ShipmentUpdatePostgres");

// var orleansStorage = configSection.GetValue<bool>("OrleansStorage");
// var adoNetGrainStorage = configSection.GetValue<bool>("AdoNetGrainStorage");
// var adoNetConnectionString = configSection.GetValue<string>("AdoNetConnectionString");
// var logRecords = configSection.GetValue<bool>("LogRecords");
// int numShipmentActors = configSection.GetValue<int>("NumShipmentActors");
// var useDash = configSection.GetValue<bool>("UseDashboard");
// var useSwagger = configSection.GetValue<bool>("UseSwagger");

// var streamReplication = configSection.GetValue<bool>("StreamReplication");
// var redisReplication = configSection.GetValue<bool>("RedisReplication");
// var redisPrimaryConnectionString = configSection.GetValue<string>("RedisPrimaryConnectionString");
// var redisSecondaryConnectionString = configSection.GetValue<string>("RedisSecondaryConnectionString");

// var trackCartHistory = configSection.GetValue<bool>("TrackCartHistory");

// AppConfig appConfig = new()
// {
//     OrleansTransactions = orleansTransactions,
//     SellerViewPostgres = sellerViewPostgres,
//     ShipmentUpdatePostgres = shipmentUpdatePostgres,
//     StreamReplication = streamReplication,
//     RedisReplication = redisReplication,
//     RedisPrimaryConnectionString = redisPrimaryConnectionString,
//     RedisSecondaryConnectionString = redisSecondaryConnectionString,
//     OrleansStorage = orleansStorage,
//     AdoNetGrainStorage = adoNetGrainStorage,
//     AdoNetConnectionString = adoNetConnectionString,
//     LogRecords = logRecords,
//     NumShipmentActors = numShipmentActors,
//     UseDashboard = useDash,
//     UseSwagger = useSwagger,
//     TrackCartHistory = trackCartHistory
// };

// // Orleans testing has no support for IOptions apparently...
// // builder.Services.Configure<AppConfig>(configSection);
// builder.Services.AddSingleton(appConfig);

// // Add services to the container.
// builder.Services.AddControllers();

// // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// if(useSwagger){
//     builder.Services.AddEndpointsApiExplorer();
//     builder.Services.AddSwaggerGen();
// }

// if (logRecords){
//     builder.Services.AddSingleton<IAuditLogger, PostgresAuditLogger>();
// } else {
//     builder.Services.AddSingleton<IAuditLogger, EtcNullPersistence>();
// }

// // in case aspnet core with orleans client: https://learn.microsoft.com/en-us/dotnet/orleans/tutorials-and-samples/tutorial-1
// builder.Host.UseOrleans(siloBuilder =>
// {
//     siloBuilder
//          .UseLocalhostClustering()
//          .ConfigureLogging(logging =>
//          {
//              logging.ClearProviders();
//              logging.AddConsole();
//              // to change minimum log level, use the following option:
//              //logging.SetMinimumLevel(LogLevel.Warning);
//          });

//     // if (sellerViewPostgres)
//     // {
//     //     siloBuilder.Services.AddDbContextFactory<SellerDbContext>();
//     //     if(shipmentUpdatePostgres)
//     //         builder.Services.AddSingleton<IShipmentService, CustomShipmentServiceImpl>();
//     //     else
//     //         builder.Services.AddSingleton<IShipmentService, DefaultShipmentServiceImpl>();
//     // } else
//     // {
//     //     builder.Services.AddSingleton<IShipmentService, DefaultShipmentServiceImpl>();
//     // }

//     if (streamReplication)
//     {
//         siloBuilder.AddMemoryStreams(Constants.DefaultStreamProvider)
//                     .AddMemoryGrainStorage(Constants.DefaultStreamStorage);
//     }

//     if (orleansTransactions)
//     {
//         siloBuilder.UseTransactions();
//         siloBuilder.Configure<ClientMessagingOptions>(options=>{
//             //options.ResponseTimeout = TimeSpan.FromMinutes(1);
//             options.ResponseTimeoutWithDebugger = TimeSpan.FromMinutes(10);
//            //options.DropExpiredMessages = true;
//         });
//         siloBuilder.Configure<SiloMessagingOptions>(options=>{
//             // options.ResponseTimeout = TimeSpan.FromMinutes(1);
//             options.ResponseTimeoutWithDebugger = TimeSpan.FromMinutes(10);
//             //options.DropExpiredMessages = true;
//         });

//         siloBuilder.Configure<TransactionalStateOptions>(options => {
//             //options.LockAcquireTimeout = TimeSpan.FromMinutes(1);
//             //options.LockTimeout = TimeSpan.FromMilliseconds(10000);
//             //options.MaxLockGroupSize = 100;
//         });
//         siloBuilder.Services.AddSerializer(ser => { ser.AddNewtonsoftJsonSerializer(isSupported: type => type.Namespace.StartsWith("OnlineMarket.Core.Common") || type.Namespace.StartsWith("OnlineMarket.OrleansImpl")); });
    
//         if (adoNetGrainStorage)
//         {
//             siloBuilder.AddAdoNetGrainStorage(Constants.OrleansStorage, options =>
//              {
//                  options.Invariant = "Npgsql";
//                  options.ConnectionString = adoNetConnectionString;
//              });
//         }
//         else
//         {
//             siloBuilder.AddMemoryGrainStorage(Constants.OrleansStorage);
//         }
    
//     } else
//     {
//         siloBuilder.Services.AddSerializer(ser => ser.AddNewtonsoftJsonSerializer(isSupported: type => type.Namespace.StartsWith("OnlineMarket.Core.Common")));

//         // required to make persistentState being injected on non-transactional grains
//         // TODO separate OrleansStorage option from actual writes to storage grain state (WriteAsync)
//         siloBuilder.AddMemoryGrainStorage(Constants.OrleansStorage);
//     }

//     if(useDash){
//       siloBuilder.UseDashboard(x => x.HostSelf = true);
//     }

//     if (redisReplication)
//     {
//         siloBuilder.Services.AddSingleton<IRedisConnectionFactory>(new RedisConnectionFactoryImpl(redisPrimaryConnectionString, redisSecondaryConnectionString));
//     } else
//     {
//         // just to avoid errors on new instances of TransactionalProductActor
//         siloBuilder.Services.AddSingleton<IRedisConnectionFactory>(new EtcNullConnectionFactoryImpl());
//     }

// });

// var app = builder.Build();

// // if (sellerViewPostgres)
// // {
// //     AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
// //     using (var scope = app.Services.CreateScope())
// //     {
// //         var services = scope.ServiceProvider;
// //         var context = services.GetRequiredService<SellerDbContext>();
// //         context.Database.Migrate();

// //         // truncate order entries on starting a new experiment
// //         context.OrderEntries.ExecuteDelete();
// //     }
// // }

// if (logRecords){
//     var persistence = app.Services.GetService<IAuditLogger>();
//     // init log table in PostgreSQL
//     await persistence.SetUpLog();
//     await persistence.CleanLog();
//     // it guarantees that, upon activating the actor, the state is null
//     await persistence.TruncateStorage();
// }

// // Configure the HTTP request pipeline.
// if (useSwagger)
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }
// if(useDash) app.Map("/dashboard", x => x.UseOrleansDashboard());

// app.MapControllers();

// await app.StartAsync();

// Console.WriteLine("\n *************************************************************************");
// Console.WriteLine(
//     " OrleansTransactions: "+ appConfig.OrleansTransactions +  
//     " \n SellerViewPostgres: " + appConfig.SellerViewPostgres +
//     " \n ShipmentUpdatePostgres: " + appConfig.ShipmentUpdatePostgres +
//     " \n OrleansStorage: " + appConfig.OrleansStorage +
//     " \n AdoNetGrainStorage: "+appConfig.AdoNetGrainStorage +
//     " \n AdoNetConnectionString: "+appConfig.AdoNetConnectionString +
//     " \n LogRecords: "+appConfig.LogRecords +
//     " \n UseSwagger: "+useSwagger +
//     " \n UseDashboard: "+appConfig.UseDashboard +
//     " \n NumShipmentActors: "+appConfig.NumShipmentActors +
//     " \n Stream Replication: " + appConfig.StreamReplication +
//     " \n RedisReplication: " + appConfig.RedisReplication +
//     " \n RedisPrimaryConnectionString: "+ appConfig.RedisPrimaryConnectionString +
//     " \n RedisSecondaryConnectionString: "+ appConfig.RedisSecondaryConnectionString +
//     " \n TrackCartHistory: "+appConfig.TrackCartHistory
//     );
// Console.WriteLine("            The Orleans server started. Press any key to terminate...         ");
// Console.WriteLine("\n *************************************************************************");

// Console.ReadLine();

// await app.StopAsync();


using Orleans.Configuration;
using OnlineMarket.OrleansImpl.Infra;
using Orleans.Serialization;
using OnlineMarket.OrleansImpl.Infra.Redis;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:8081");

IConfigurationSection configSection = builder.Configuration.GetSection("AppConfig");

var orleansTransactions = configSection.GetValue<bool>("OrleansTransactions");
var sellerViewPostgres = configSection.GetValue<bool>("SellerViewPostgres");
var shipmentUpdatePostgres = configSection.GetValue<bool>("ShipmentUpdatePostgres");

var orleansStorage = configSection.GetValue<bool>("OrleansStorage");
var adoNetGrainStorage = configSection.GetValue<bool>("AdoNetGrainStorage");
var adoNetConnectionString = configSection.GetValue<string>("AdoNetConnectionString");
var logRecords = configSection.GetValue<bool>("LogRecords");
int numShipmentActors = configSection.GetValue<int>("NumShipmentActors");
var useDash = configSection.GetValue<bool>("UseDashboard");
var useSwagger = configSection.GetValue<bool>("UseSwagger");

var streamReplication = configSection.GetValue<bool>("StreamReplication");
var redisReplication = configSection.GetValue<bool>("RedisReplication");
var redisPrimaryConnectionString = configSection.GetValue<string>("RedisPrimaryConnectionString");
var redisSecondaryConnectionString = configSection.GetValue<string>("RedisSecondaryConnectionString");

var trackCartHistory = configSection.GetValue<bool>("TrackCartHistory");

AppConfig appConfig = new()
{
    OrleansTransactions = orleansTransactions,
    SellerViewPostgres = sellerViewPostgres,
    ShipmentUpdatePostgres = shipmentUpdatePostgres,
    StreamReplication = streamReplication,
    RedisReplication = redisReplication,
    RedisPrimaryConnectionString = redisPrimaryConnectionString,
    RedisSecondaryConnectionString = redisSecondaryConnectionString,
    OrleansStorage = orleansStorage,
    AdoNetGrainStorage = adoNetGrainStorage,
    AdoNetConnectionString = adoNetConnectionString,
    LogRecords = logRecords,
    NumShipmentActors = numShipmentActors,
    UseDashboard = useDash,
    UseSwagger = useSwagger,
    TrackCartHistory = trackCartHistory
};

builder.Services.AddSingleton(appConfig);
builder.Services.AddControllers();

if (useSwagger)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

if (logRecords)
{
    builder.Services.AddSingleton<IAuditLogger, PostgresAuditLogger>();
}
else
{
    builder.Services.AddSingleton<IAuditLogger, EtcNullPersistence>();
}

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering()
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

    if (sellerViewPostgres)
    {
        // ✅ 添加 DbContextFactory 解决 SellerViewActor 注入问题
        siloBuilder.Services.AddDbContextFactory<OnlineMarket.OrleansImpl.Infra.SellerDb.SellerDbContext>();
    }

    if (streamReplication)
    {
        siloBuilder.AddMemoryStreams(Constants.DefaultStreamProvider)
            .AddMemoryGrainStorage(Constants.DefaultStreamStorage);
    }

    if (orleansTransactions)
    {
        siloBuilder.UseTransactions();
        siloBuilder.Configure<ClientMessagingOptions>(options => {
            options.ResponseTimeoutWithDebugger = TimeSpan.FromMinutes(10);
        });
        siloBuilder.Configure<SiloMessagingOptions>(options => {
            options.ResponseTimeoutWithDebugger = TimeSpan.FromMinutes(10);
        });

        siloBuilder.Services.AddSerializer(ser => {
            ser.AddNewtonsoftJsonSerializer(isSupported: type =>
                type.Namespace.StartsWith("OnlineMarket.Core.Common") ||
                type.Namespace.StartsWith("OnlineMarket.OrleansImpl"));
        });

        if (adoNetGrainStorage)
        {
            // ✅ 用 AdoNetGrainStorage 避免 MemoryStorage eTag 冲突
            siloBuilder.AddAdoNetGrainStorage(Constants.OrleansStorage, options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = adoNetConnectionString;
            });
        }
        else
        {
            siloBuilder.AddMemoryGrainStorage(Constants.OrleansStorage);
        }
    }
    else
    {
        siloBuilder.Services.AddSerializer(ser =>
            ser.AddNewtonsoftJsonSerializer(isSupported: type =>
                type.Namespace.StartsWith("OnlineMarket.Core.Common")));

        siloBuilder.AddMemoryGrainStorage(Constants.OrleansStorage);
    }

    if (useDash)
    {
        siloBuilder.UseDashboard(x => x.HostSelf = true);
    }

    if (redisReplication)
    {
        siloBuilder.Services.AddSingleton<IRedisConnectionFactory>(
            new RedisConnectionFactoryImpl(redisPrimaryConnectionString, redisSecondaryConnectionString));
    }
    else
    {
        siloBuilder.Services.AddSingleton<IRedisConnectionFactory>(new EtcNullConnectionFactoryImpl());
    }
});

var app = builder.Build();

if (logRecords)
{
    var persistence = app.Services.GetService<IAuditLogger>();
    await persistence.SetUpLog();
    await persistence.CleanLog();
    await persistence.TruncateStorage();
}

if (useSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (useDash)
{
    app.Map("/dashboard", x => x.UseOrleansDashboard());
}

app.MapControllers();

await app.StartAsync();

Console.WriteLine("\n *************************************************************************");
Console.WriteLine(
    " OrleansTransactions: " + appConfig.OrleansTransactions +
    " \n SellerViewPostgres: " + appConfig.SellerViewPostgres +
    " \n ShipmentUpdatePostgres: " + appConfig.ShipmentUpdatePostgres +
    " \n OrleansStorage: " + appConfig.OrleansStorage +
    " \n AdoNetGrainStorage: " + appConfig.AdoNetGrainStorage +
    " \n AdoNetConnectionString: " + appConfig.AdoNetConnectionString +
    " \n LogRecords: " + appConfig.LogRecords +
    " \n UseSwagger: " + useSwagger +
    " \n UseDashboard: " + appConfig.UseDashboard +
    " \n NumShipmentActors: " + appConfig.NumShipmentActors +
    " \n Stream Replication: " + appConfig.StreamReplication +
    " \n RedisReplication: " + appConfig.RedisReplication +
    " \n RedisPrimaryConnectionString: " + appConfig.RedisPrimaryConnectionString +
    " \n RedisSecondaryConnectionString: " + appConfig.RedisSecondaryConnectionString +
    " \n TrackCartHistory: " + appConfig.TrackCartHistory
);
Console.WriteLine("            The Orleans server started. Press any key to terminate...");
Console.WriteLine("\n *************************************************************************");

Console.ReadLine();

await app.StopAsync();
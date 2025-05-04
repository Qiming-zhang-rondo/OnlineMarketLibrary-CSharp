// using OnlineMarket.Core.Services;
// using OnlineMarket.Core.Common.Config;
// using OnlineMarket.OrleansImpl.Interfaces;
// using OnlineMarket.OrleansImpl.Infra.Adapter;
// using OnlineMarket.OrleansImpl.Infra.SellerDb;
// using Microsoft.Extensions.Logging;
// using Microsoft.EntityFrameworkCore;
//
//
// namespace OnlineMarket.OrleansImpl.Grains;
//
// public sealed class CustomShipmentService : ICustomShipmentService
// {
//     private readonly CustomShipmentServiceCore _core;
//
//     public CustomShipmentService(AppConfig cfg,
//         IDbContextFactory<SellerDbContext> dbf,
//         IGrainFactory gf,
//         ILogger<CustomShipmentServiceCore> log)
//     {
//         var repo = new EfOrderEntryRepository(dbf);
//         var bus  = new OrleansShipmentDispatcher(gf, cfg.OrleansTransactions);
//         var part = new ModPartitioner(cfg.NumShipmentActors);
//         _core = new CustomShipmentServiceCore(
//             repo, bus, part, SystemClock.Instance, log, take:10);
//     }
//
//     public Task UpdateShipment(string instanceId) =>
//         _core.UpdateShipment(instanceId);
// }

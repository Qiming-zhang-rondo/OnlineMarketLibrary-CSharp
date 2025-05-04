// using OnlineMarket.Core.Ports;
// using OnlineMarket.Core.Common.Events;
// using Microsoft.Extensions.Logging;  
//
// namespace OnlineMarket.Core.Services;
//
// // OnlineMarket.Core.Services/CustomShipmentServiceCore.cs
// public sealed class CustomShipmentServiceCore
// {
//     private readonly IOrderEntryRepository _repo;
//     private readonly IShipmentBus          _bus;
//     private readonly IPartitioner          _part;
//     private readonly IClock                _clock;
//     private readonly ILogger               _log;
//     private readonly int                   _take;
//
//     public CustomShipmentServiceCore(
//         IOrderEntryRepository repo,
//         IShipmentBus bus,
//         IPartitioner part,
//         IClock clock,
//         ILogger log,
//         int take = 10)
//     {
//         _repo  = repo;
//         _bus   = bus;
//         _part  = part;
//         _clock = clock;
//         _log   = log;
//         _take  = take;
//     }
//
//     public async Task UpdateShipment(string instanceId)
//     {
//         // 1) 取批次
//         var entries = await _repo.GetNextBatchAsync(_take);
//         if (entries.Count == 0)
//             throw new ApplicationException("No order entries were retrieved from the database!");
//
//         // 2) 按 partitionId 分桶
//         var dict = new Dictionary<int, HashSet<(int,int,int)>>();
//         foreach (var e in entries)
//         {
//             int pid = _part.ResolvePartition(e.customer_id);
//             dict.TryAdd(pid, new());
//             dict[pid].Add((e.customer_id, e.order_id, e.seller_id));
//         }
//
//         _log.LogInformation("{Count} order entries retrieved from database.",
//             dict.Sum(kv => kv.Value.Count));
//
//         // 3) 分发
//         var tasks = dict.Select(kv => _bus.DispatchAsync(kv.Key, instanceId, kv.Value));
//         await Task.WhenAll(tasks);
//     }
// }
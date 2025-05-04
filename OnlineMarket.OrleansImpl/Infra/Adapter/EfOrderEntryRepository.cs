// using OnlineMarket.Core.Ports;
// using OnlineMarket.Core.Common.Entities;
// using Microsoft.EntityFrameworkCore;
// using OnlineMarket.OrleansImpl.Infra.SellerDb;
//
//
// namespace OnlineMarket.OrleansImpl.Infra.Adapter;
//
//
// public sealed class EfOrderEntryRepository : IOrderEntryRepository
// {
//     private readonly IDbContextFactory<SellerDbContext> _factory;
//     private const string Sql =
//         "SELECT * FROM public.order_entries LIMIT {0} FOR UPDATE SKIP LOCKED";
//
//     public EfOrderEntryRepository(IDbContextFactory<SellerDbContext> f) => _factory = f;
//
//     public async Task<IReadOnlyCollection<OrderEntry>> GetNextBatchAsync(int take)
//     {
//         using var db = _factory.CreateDbContext();
//         using var tx = await db.Database.BeginTransactionAsync();
//         var result   = db.OrderEntries.FromSqlRaw(string.Format(Sql, take)).ToList();
//         await tx.CommitAsync();       // 提前释放锁
//         return result;
//     }
// }
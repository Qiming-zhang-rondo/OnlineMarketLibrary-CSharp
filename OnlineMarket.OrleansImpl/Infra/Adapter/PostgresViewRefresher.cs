using Microsoft.EntityFrameworkCore;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Infra.SellerDb;


namespace OnlineMarket.OrleansImpl.Infra.Adapter;
//物化视图刷新器

public sealed class PostgresViewRefresher : IMaterializedViewRefresher
{
    private readonly IDbContextFactory<SellerDbContext> _factory;
    public PostgresViewRefresher(IDbContextFactory<SellerDbContext> f) => _factory = f;

    public async Task RefreshAsync(int sellerId)
    {
        await using var db = _factory.CreateDbContext();
        // await db.Database
        //     .ExecuteSqlRawAsync($"CALL refresh_order_view({sellerId})");
        await db.Database
            .ExecuteSqlRawAsync(
                SellerDbContext.GetRefreshCustomOrderSellerViewSql(sellerId)
            );
    }
}

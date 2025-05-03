namespace OnlineMarket.Core.Ports;

// Ports/IMaterializedViewRefresher.cs
public interface IMaterializedViewRefresher
{
    Task RefreshAsync(int sellerId);
}
// Ports/IClock.cs
namespace OnlineMarket.Core.Ports
{
    /// Replaceable and testable clock
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
// ProductMS/Infrastructure/SystemClock.cs
using OnlineMarket.Core.Ports;

namespace ProductMS.Infrastructure;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
using OnlineMarket.Core.Ports;

namespace CartMS.Infrastructure;

/// <summary>生产环境用系统时钟。</summary>
internal sealed class SystemClock : IClock
{
    public global::System.DateTime UtcNow => global::System.DateTime.UtcNow;
}
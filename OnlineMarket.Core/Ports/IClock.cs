// Ports/IClock.cs
namespace OnlineMarket.Core.Ports
{
    /// <summary>可替换、可测试的时钟。</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
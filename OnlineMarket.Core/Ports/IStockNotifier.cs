// Ports/IStockNotifier.cs
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Ports
{
    /// <summary>库存系统通知端口。</summary>
    public interface IStockNotifier
    {
        Task NotifyProductUpdated(ProductUpdated evt);
    }
}
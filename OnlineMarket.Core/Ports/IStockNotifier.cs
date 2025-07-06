// Ports/IStockNotifier.cs
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Ports
{
    public interface IStockNotifier
    {
        Task NotifyProductUpdated(ProductUpdated evt);
    }
}
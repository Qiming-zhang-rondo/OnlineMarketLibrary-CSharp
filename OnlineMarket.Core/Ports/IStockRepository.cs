// OnlineMarket.Core.Ports/IStockRepository.cs
using OnlineMarket.Core.Common.Entities;
namespace OnlineMarket.Core.Ports
{
    public interface IStockRepository
    {
        Task SaveAsync(StockItem item);
    }
}

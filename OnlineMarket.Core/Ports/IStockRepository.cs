// OnlineMarket.Core.Ports/IStockRepository.cs
using OnlineMarket.Core.Common.Entities;
namespace OnlineMarket.Core.Ports
{
    public interface IStockRepository
    {
        Task SaveAsync(StockItem item);
    }
}

// 可复用之前的 IClock（若要可测试时钟）。
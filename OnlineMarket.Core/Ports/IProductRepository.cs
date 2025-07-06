// Ports/IProductRepository.cs
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Ports
{
    /// Persistence port: save Product to storage (database / OrleansState / file, etc.)
    public interface IProductRepository
    {
        Task SaveAsync(Product product);
    }
}
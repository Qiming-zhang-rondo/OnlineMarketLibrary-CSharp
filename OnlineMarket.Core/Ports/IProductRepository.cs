// Ports/IProductRepository.cs
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Ports
{
    /// <summary>持久化端口：把 Product 保存到存储（数据库 / OrleansState / 文件 等）。</summary>
    public interface IProductRepository
    {
        Task SaveAsync(Product product);
    }
}
// 接口定义：OnlineMarket.Core.Interfaces/IProductService.cs
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using System.Threading.Tasks;

namespace OnlineMarket.Core.Interfaces
{
    public interface IProductService
    {
        
        /// Initialize and publish a new product.
        Task SetProduct(Product product);
        
        /// Handle complete updates of products pushed externally
        Task ProcessProductUpdate(Product product);
        
        /// Get the current persistent product status
        Task<Product> GetProduct();
        
        /// Update Price
        Task ProcessPriceUpdate(PriceUpdate priceUpdate);
        
        /// Reset
        Task Reset();
    }
}
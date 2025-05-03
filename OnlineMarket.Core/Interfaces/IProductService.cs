// 接口定义：OnlineMarket.Core.Interfaces/IProductService.cs
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using System.Threading.Tasks;

namespace OnlineMarket.Core.Interfaces
{
    public interface IProductService
    {
        /// <summary>
        /// 初始化并发布一个新商品。
        /// </summary>
        Task SetProduct(Product product);

        /// <summary>
        /// 处理外部推送的商品完整更新（包含版本、价格、元数据等）。
        /// </summary>
        Task ProcessProductUpdate(Product product);

        /// <summary>
        /// 获取当前持久化的商品状态。
        /// </summary>
        Task<Product> GetProduct();

        /// <summary>
        /// 仅更新商品价格并同步到存储/流/缓存。
        /// </summary>
        Task ProcessPriceUpdate(PriceUpdate priceUpdate);

        /// <summary>
        /// 重置商品版本号并更新时间戳。
        /// </summary>
        Task Reset();
    }
}
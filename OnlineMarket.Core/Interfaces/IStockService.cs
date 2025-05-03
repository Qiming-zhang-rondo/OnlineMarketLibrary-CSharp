// OnlineMarket.Core.Interfaces/IStockService.cs
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;  // 包含 StockItem、CartItem、ItemStatus
using OnlineMarket.Core.Common.Events;    // 包含 ProductUpdated

namespace OnlineMarket.Core.Interfaces
{
    /// <summary>
    /// 核心领域的库存服务接口，不依赖 Orleans 实现细节。
    /// </summary>
    public interface IStockService
    {
        /// <summary>
        /// 初始化或重置某个库存条目。
        /// </summary>
        Task SetItem(StockItem item);

        /// <summary>
        /// 尝试为购物项保留库存，返回保留结果状态。
        /// </summary>
        Task<ItemStatus> AttemptReservation(CartItem cartItem);

        /// <summary>
        /// 取消先前的库存保留。
        /// </summary>
        Task CancelReservation(int quantity);

        /// <summary>
        /// 确认先前的库存保留（实际扣减可用量）。
        /// </summary>
        Task ConfirmReservation(int quantity);

        /// <summary>
        /// 当商品元数据（版本号）更新时，同步更新库存项的版本号。
        /// </summary>
        Task ProcessProductUpdate(ProductUpdated productUpdated);

        /// <summary>
        /// 获取当前库存条目状态。
        /// </summary>
        Task<StockItem> GetItem();

        /// <summary>
        /// 将库存重置到初始状态（保留 0，可用 10000，版本 "0"）。
        /// </summary>
        Task Reset();
    }
}
// OnlineMarket.Core.Interfaces/ICartService.cs
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;

namespace OnlineMarket.Core.Interfaces
{
    /// <summary>
    /// 购物车应用服务（纯业务，无任何 Orleans / EF 依赖）
    /// </summary>
    public interface ICartService
    {
        /*──────── 查询 —— 只读 ────────*/

        /// <summary>获取当前整车快照。</summary>
        Task<Cart> GetCartAsync();

        /// <summary>仅取购物车条目。</summary>
        Task<IReadOnlyList<CartItem>> GetItemsAsync();

        /// <summary>若开启 trackHistory，可按 transaction‑id 回溯当时快照。</summary>
        Task<IReadOnlyList<CartItem>> GetHistoryAsync(string tid);

        /*──────── 命令 —— 会修改状态 ────────*/

        /// <summary>向购物车添加一条商品。</summary>
        Task AddItemAsync(CartItem item);

        /// <summary>
        /// 用户确认结账。  
        /// 实现应：
        /// <list type="number">
        ///   <item>把 Cart 状态置为 <c>CHECKOUT_SENT</c>；</item>
        ///   <item>调用 <see cref="IOrderGateway"/> 执行结账流程；</item>
        ///   <item>成功后自动 <see cref="SealAsync"/> 清空购物车。</item>
        /// </list>
        /// </summary>
        Task NotifyCheckoutAsync(CustomerCheckout checkout);

        /// <summary>手动清空购物车并回到 <c>OPEN</c> 状态。</summary>
        Task SealAsync();

        /// <summary>开发／测试用：彻底重置购物车。</summary>
        Task ResetAsync();
    }
}
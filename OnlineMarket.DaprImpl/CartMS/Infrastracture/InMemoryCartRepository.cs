// CartMS/Repository/InMemoryCartRepository.cs
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CartMS.Repository;

/// <summary>
/// 纯内存存储版购物车仓库，实现 <see cref="ICartRepository"/>。
/// </summary>
internal sealed class InMemoryCartRepository : ICartRepository
{
    // key = customerId , value = Cart
    private readonly ConcurrentDictionary<int, Cart> _store = new();

    /*──────── ICartRepository 实现 ────────*/

    /// <summary>加载购物车；不存在则返回 <c>null</c>。</summary>
    public Task<Cart?> LoadAsync(int customerId)
    {
        _store.TryGetValue(customerId, out var cart);
        return Task.FromResult(cart);
    }

    /// <summary>保存（插入或覆盖）。</summary>
    public Task SaveAsync(Cart cart)
    {
        _store[cart.customerId] = cart;
        return Task.CompletedTask;
    }

    /// <summary>删除整车。</summary>
    public Task ClearAsync(int customerId)
    {
        _store.TryRemove(customerId, out _);
        return Task.CompletedTask;
    }

    /*──────── 额外工具方法（可选） ────────*/

    /// <summary>开发调试用：清空所有数据。</summary>
    public void Reset() => _store.Clear();
}
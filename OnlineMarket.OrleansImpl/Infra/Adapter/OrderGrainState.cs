// OnlineMarket.OrleansImpl.Infra.Adapter/OrderGrainState.cs
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;

/// <summary>
/// Orleans MemoryStorage 内部保存的订单复合状态。
/// </summary>
public sealed class OrderGrainState
{
    public Order                 Order   { get; set; } = default!;
    public List<OrderItem>       Items   { get; set; } = new();
    public List<OrderHistory>    History { get; set; } = new();
}

/// <summary>简单自增计数器，替代旧的 NextOrderIdState。</summary>
public sealed class OrderIdCounter
{
    public int Value { get; set; }
    public OrderIdCounter GetNext() { Value++; return this; }
}
// OnlineMarket.OrleansImpl.Infra.Adapter/OrderGrainState.cs
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.OrleansImpl.Infra.Adapter;


// The composite state of the order is stored internally in Orleans MemoryStorage.
public sealed class OrderGrainState
{
    public Order                 Order   { get; set; } = default!;
    public List<OrderItem>       Items   { get; set; } = new();
    public List<OrderHistory>    History { get; set; } = new();
}

//Simple self-increment counter, replacing the old NextOrderIdState
public sealed class OrderIdCounter
{
    public int Value { get; set; }
    public OrderIdCounter GetNext() { Value++; return this; }
}
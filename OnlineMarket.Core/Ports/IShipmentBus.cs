namespace OnlineMarket.Core.Ports;

// Ports/IShipmentBus.cs
public interface IShipmentBus
{
    //把一批 (customerId,orderId,sellerId) 条目真正下发出去”
    //把这些条目分发到对应分区的 Grain（或者事务 Grain）来做 UpdateShipment(...)
    Task DispatchAsync(int partitionId,
        string instanceId,
        ISet<(int customerId,int orderId,int sellerId)> entries);
}
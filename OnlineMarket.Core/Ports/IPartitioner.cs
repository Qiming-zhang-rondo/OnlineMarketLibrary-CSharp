namespace OnlineMarket.Core.Ports;

// Ports/IPartitioner.cs
public interface IPartitioner
{
    //给定一个 customerId，算出它应该发送到哪个 ShipmentActor 分区
    int ResolvePartition(int customerId);
}
// using OnlineMarket.Core.Common.Entities;
//
// namespace OnlineMarket.Core.Common.Events;
//
// public class ShipmentNotification
// {
//     public int customerId { get; set; }
//     public int orderId { get; set; }
//     public DateTime eventDate { get; set; }
//     public string instanceId { get; set; }
//     public ShipmentStatus status { get; set; }
//
//     public ShipmentNotification(){ }
//
//     public ShipmentNotification(int customerId, int orderId, DateTime eventDate, string instanceId, ShipmentStatus status,)
//     {
//         this.customerId = customerId;
//         this.orderId = orderId;
//         this.eventDate = eventDate;
//         this.instanceId = instanceId;
//         this.status = status;
//     }
// }
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Common.Events;

/// <summary>
/// 物流事件通知：不可变 record，支持 with-copy。
/// </summary>
public record ShipmentNotification
{
    public int CustomerId   { get; init; }
    public int OrderId      { get; init; }
    public DateTime EventDate { get; init; }
    public string InstanceId  { get; init; } = null!;
    public ShipmentStatus Status   { get; init; }
    public int SellerId     { get; init; }      // ← 新增字段

    public ShipmentNotification() { }

    public ShipmentNotification(
        int customerId,
        int orderId,
        DateTime eventDate,
        string instanceId,
        ShipmentStatus status,
        int sellerId)
    {
        CustomerId = customerId;
        OrderId    = orderId;
        EventDate  = eventDate;
        InstanceId = instanceId;
        Status     = status;
        SellerId   = sellerId;
    }
}

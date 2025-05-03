// OnlineMarket.Core.Common.Entities/NextShipmentIdState.cs
namespace OnlineMarket.Core.Common.Entities;

public class NextShipmentIdState
{
    public int Value { get; set; }
    public NextShipmentIdState()           => Value = 0;
    public NextShipmentIdState(int value)  => Value = value;

    public NextShipmentIdState GetNextShipmentId()
    {
        Value++;
        return this;
    }
}
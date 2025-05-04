namespace OnlineMarket.OrleansImpl.Infra;

public static class Helper
{

    // public static int GetShipmentActorId(int customerId, int numShipmentActors)
    // {
    //     var n = numShipmentActors > 0 ? numShipmentActors : 1;
    //     return customerId % n;
    // }
    public static int GetShipmentActorId(int customerId, int numShipmentActors) => customerId % numShipmentActors;
   
}
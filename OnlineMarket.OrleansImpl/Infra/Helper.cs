namespace OnlineMarket.OrleansImpl.Infra;

public static class Helper
{

    public static int GetShipmentActorId(int customerId, int numShipmentActors) => customerId % numShipmentActors;
   
}
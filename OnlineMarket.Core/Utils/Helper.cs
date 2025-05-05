namespace OnlineMarket.Core.Utils
{
    public static class Helper
    {
        public static int GetShipmentGroupId(int customerId, int numShipmentGroups)
        {
            return customerId % numShipmentGroups;
        }
    }
}
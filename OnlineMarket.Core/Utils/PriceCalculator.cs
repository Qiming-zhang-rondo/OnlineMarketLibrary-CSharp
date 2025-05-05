using System.Collections.Generic;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Utils;

public static class PriceCalculator
{
    public static (float totalItems, float totalAmount, float totalIncentive, Dictionary<(int, int), float> itemTotals) CalculateTotals(List<CartItem> items)
    {
        float totalItems = 0;
        float totalAmount = 0;
        float totalIncentive = 0;
        Dictionary<(int, int), float> itemTotals = new();

        foreach (var item in items)
        {
            float itemTotal = item.UnitPrice * item.Quantity;
            totalItems += itemTotal;

            if (itemTotal - item.Voucher > 0)
            {
                totalAmount += (itemTotal - item.Voucher);
                totalIncentive += item.Voucher;
                itemTotals.Add((item.SellerId, item.ProductId), itemTotal - item.Voucher);
            }
            else
            {
                totalIncentive += itemTotal;
                itemTotals.Add((item.SellerId, item.ProductId), 0);
            }
        }

        return (totalItems, totalAmount, totalIncentive, itemTotals);
    }
}
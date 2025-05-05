using System.Collections.Generic;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;

namespace OnlineMarket.Core.Utils
{
    public static class OrderItemMapper
    {
        public static List<OrderItem> MapFromCartItems(List<CartItem> cartItems, int orderId, Dictionary<(int, int), float> totalPerItem)
        {
            var items = new List<OrderItem>();
            int id = 1;

            foreach (var item in cartItems)
            {
                items.Add(new OrderItem
                {
                    order_id = orderId,
                    order_item_id = id++,
                    product_id = item.ProductId,
                    product_name = item.ProductName,
                    seller_id = item.SellerId,
                    unit_price = item.UnitPrice,
                    quantity = item.Quantity,
                    total_items = item.UnitPrice * item.Quantity,
                    total_amount = totalPerItem[(item.SellerId, item.ProductId)],
                    freight_value = item.FreightValue,
                    shipping_limit_date = DateTime.UtcNow.AddDays(3),
                    voucher = item.Voucher
                });
            }

            return items;
        }
    }
}
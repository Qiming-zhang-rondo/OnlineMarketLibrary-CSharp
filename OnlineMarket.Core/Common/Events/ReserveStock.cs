using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;

namespace OnlineMarket.Core.Common.Events;

public class ReserveStock
{
    public DateTime timestamp {get; set; }

    public CustomerCheckout customerCheckout {get; set; }

    public List<CartItem> items {get; set; }

    public string instanceId {get; set; }

    public ReserveStock(){ }

    public ReserveStock(DateTime timestamp, CustomerCheckout customerCheckout, List<CartItem> items, string instanceId)
    {
        this.timestamp = timestamp;
        this.customerCheckout = customerCheckout;
        this.items = items;
        this.instanceId = instanceId;
    }

    public override string ToString()
    {
        return string.Join(",",items);
    }
}

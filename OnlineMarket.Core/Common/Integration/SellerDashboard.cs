using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.Core.Common.Integration;

public sealed class SellerDashboard
{
	public OrderSellerView SellerView { get; set; }
	public List<OrderEntry> OrderEntries { get; set; }

    public SellerDashboard(){ }

    public SellerDashboard(OrderSellerView sellerView, List<OrderEntry> orderEntries)
    {
        this.SellerView = sellerView;
        this.OrderEntries = orderEntries;
    }
}


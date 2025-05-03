namespace OnlineMarket.Core.Ports;

public interface IStockGateway
{
    Task ConfirmAsync(int sellerId, int productId, int qty);
}
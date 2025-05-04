using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Ports;

// OnlineMarket.Core.Ports/IOrderGateway.cs
public interface IOrderGateway
{
    Task CheckoutAsync(ReserveStock rs);
}
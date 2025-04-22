using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Common.Requests;

using System.Threading.Tasks;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.Core.Common.Entities;

namespace OnlineMarket.OrleansImpl.Tests.Infra.Mocks
{
    public class FakeOrderActorAdapter : IOrderService
{
    public Task Checkout(ReserveStock reserveStock)
    {
        // 模拟成功
        return Task.CompletedTask;
    }

    public Task ProcessPaymentConfirmed(PaymentConfirmed paymentConfirmed)
    {
        return Task.CompletedTask;
    }

    public Task ProcessPaymentFailed(PaymentFailed paymentFailed)
    {
        return Task.CompletedTask;
    }

    public Task ProcessShipmentNotification(ShipmentNotification shipmentNotification)
    {
        return Task.CompletedTask;
    }

    public Task<List<Order>> GetOrders()
    {
        return Task.FromResult(new List<Order>());
    }

    public Task<int> GetNumOrders()
    {
        return Task.FromResult(0);
    }

    public Task Reset()
    {
        return Task.CompletedTask;
    }

        Task<List<Order>> IOrderService.GetOrders()
        {
            throw new NotImplementedException();
        }
    }
}
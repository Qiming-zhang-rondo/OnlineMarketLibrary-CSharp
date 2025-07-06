// OnlineMarket.Core.Interfaces/ICustomerService.cs

using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Interfaces
{
    /// <summary>
    /// Domain service interface of Customer aggregate root
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>Write (or replace) the complete Customer data</summary>
        Task SetCustomer(Customer customer);

        /// <summary>Clear</summary>
        Task Clear();

        /// <summary>读取当前快照。</summary>
        Task<Customer> GetCustomer();

        /// <summary>收到“支付成功”事件，递增成功计数。</summary>
        Task NotifyPaymentConfirmed(PaymentConfirmed evt);

        /// <summary>收到“支付失败”事件，递增失败计数。</summary>
        Task NotifyPaymentFailed(PaymentFailed evt);

        /// <summary>收到“包裹送达”事件，递增送达计数。</summary>
        Task NotifyDelivery(DeliveryNotification evt);
    }
}
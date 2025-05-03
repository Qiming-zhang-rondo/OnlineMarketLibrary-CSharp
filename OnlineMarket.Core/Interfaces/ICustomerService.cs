// OnlineMarket.Core.Interfaces/ICustomerService.cs
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;

namespace OnlineMarket.Core.Interfaces
{
    /// <summary>
    /// Customer聚合根的领域服务接口——纯粹的业务契约。
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>写入（或替换）完整的 Customer 资料。</summary>
        Task SetCustomer(Customer customer);

        /// <summary>将所有统计数据清零并移除存储记录。</summary>
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
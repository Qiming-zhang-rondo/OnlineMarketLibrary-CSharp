// OnlineMarket.OrleansImpl/Grains/TransactionalPaymentActor.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Services;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans;
using Orleans.Transactions.Abstractions;

// [Reentrant]   // ➜ 避免在事务里死锁，可保留/去掉
public sealed class TransactionalPaymentActor
    : Grain, ITransactionalPaymentActor
{
    private PaymentServiceCore _core = default!;   // 延迟初始化

    // ❶ 通过 DI 直接把所有 Port 适配器注入进来 -----------------------------
    private readonly IStockGateway      _stock;
    private readonly ISellerNotifier    _sellerNtfy;
    private readonly IOrderNotifier     _orderNtfy;
    private readonly ICustomerNotifier  _custNtfy;
    private readonly IShipmentGateway   _ship;
    private readonly IClock             _clock;
    private readonly ILogger            _log;

    public TransactionalPaymentActor(
        IStockGateway      stock,
        ISellerNotifier    sellerNtfy,
        IOrderNotifier     orderNtfy,
        ICustomerNotifier  custNtfy,
        IShipmentGateway   ship,
        IClock             clock,
        ILogger<TransactionalPaymentActor> log)
    {
        _stock      = stock;
        _sellerNtfy = sellerNtfy;
        _orderNtfy  = orderNtfy;
        _custNtfy   = custNtfy;
        _ship       = ship;
        _clock      = clock;
        _log        = log;
    }

    // ❷ Grain 激活时，根据本 Grain 的 PrimaryKey 初始化 Core 层业务 ----------
    public Task OnActivateAsync()
    {
        var customerId = (int)this.GetPrimaryKeyLong();
        _core = new PaymentServiceCore(
                    customerId,
                    _stock, _sellerNtfy, _orderNtfy,
                    _custNtfy, _ship,
                    _clock, _log);

        return Task.CompletedTask;
    }

    // ❸ 接口实现 —— 只是把调用转给 Core 层 ------------------------------
    [Transaction(TransactionOption.Join)]
    public Task ProcessPayment(InvoiceIssued invoice)
        => _core.ProcessPaymentAsync(invoice);
}

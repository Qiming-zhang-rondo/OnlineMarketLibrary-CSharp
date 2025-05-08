// OnlineMarket.OrleansImpl.Interfaces/Transactional/ITransactionalOrderActor.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using Orleans.Concurrency;             // TransactionAttribute
using System.Transactions;

namespace OnlineMarket.OrleansImpl.Interfaces;

/// <summary>
/// 支持 Orleans Transactions 的订单粒度
/// </summary>
public interface ITransactionalOrderActor : IOrderActor
{
    /*───────── 写模型 (Create / Join) ─────────*/

    [Transaction(TransactionOption.Create)]
    new Task Checkout(ReserveStock reserveStock);

    [Transaction(TransactionOption.Join)]
    new Task ProcessPaymentConfirmed(PaymentConfirmed evt);

    [Transaction(TransactionOption.Join)]
    new Task ProcessPaymentFailed(PaymentFailed evt);

    [Transaction(TransactionOption.Join)]
    new Task ProcessShipmentNotification(ShipmentNotification evt);

    /*───────── 读模型 (CreateOrJoin) ─────────*/

    [Transaction(TransactionOption.CreateOrJoin)]
    new Task<List<Order>> GetOrders();

    [Transaction(TransactionOption.CreateOrJoin)]
    new Task<int> GetNumOrders();

    /*───────── 测试 / 运维 ─────────*/

    [Transaction(TransactionOption.Create)]
    new Task Reset();
}
// Tests/Order/TransactionalOrderActorTests.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using OnlineMarket.OrleansImpl.Tests.Infra.Transactional;
using OnlineMarket.OrleansImpl.Interfaces;    // ITransactionalOrderActor / ITransactionalStockActor …
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using Orleans.TestingHost;

namespace OnlineMarket.OrleansImpl.Tests.Order;

[Collection(TransactionalClusterCollection.Name)]
public class TransactionalOrderActorTests
{
    private readonly TestCluster _cluster;
    public TransactionalOrderActorTests(TransactionalClusterFixture fx) => _cluster = fx.Cluster;

    /*─────────── 帮助构造器 ───────────*/
    private static CartItem Item(int sid,int pid,int qty = 1) => new()
    {
        SellerId     = sid,
        ProductId    = pid,
        ProductName  = "Demo",
        UnitPrice    = 10,
        FreightValue = 2,
        Quantity     = qty,
        Voucher      = 0,
        Version      = "v1"
    };

    private static StockItem Stock(int sid,int pid,int qty = 100) => new()
    {
        seller_id     = sid,
        product_id    = pid,
        qty_available = qty,
        qty_reserved  = 0,
        version       = "v1",
        created_at    = DateTime.UtcNow
    };

    private static CustomerCheckout Cust(int cid,string tid) => new()
    {
        CustomerId  = cid,
        instanceId  = tid,
        PaymentType = PaymentType.CREDIT_CARD.ToString()
    };

    /*─────────────────────────────────────────────*/
    [Fact]
    public async Task Full_Checkout_Flow_Should_Work_With_Transactions()
    {
        /* 参数 */
        int cid = 1001, sid = 501, pid = 8001;

        /* ① 预置库存（TransactionalStockActor） */
        //IStockActor 还是 ITransactionalStockActor
        var stock = _cluster.GrainFactory.GetGrain<IStockActor>(sid, pid.ToString());
        await stock.SetItem(Stock(sid, pid));

        /* ② 创建购物车并结算（TransactionalCartActor → OrderActor） */
        var cart  = _cluster.GrainFactory.GetGrain<ICartActor>(cid);
        await cart.AddItem(Item(sid, pid));

        string tid = Guid.NewGuid().ToString();
        await cart.NotifyCheckout(Cust(cid, tid));

        /* 给 Orleans Tx 事务链一点时间完成 */
        await Task.Delay(200);

        /* ③ 断言：Order 已创建且状态 = INVOICED */
        var orderActor = _cluster.GrainFactory.GetGrain<ITransactionalOrderActor>(cid);
        var orders = await orderActor.GetOrders();

        Assert.Single(orders);
        Assert.Equal(OrderStatus.INVOICED, orders[0].status);

        /* ④ 支付完成 → 事务内更新状态 */
        var payEvt = new PaymentConfirmed(Cust(cid, tid), orders[0].id,
                                          orders[0].total_invoice,
                                          new(), DateTime.UtcNow, tid);
        await orderActor.ProcessPaymentConfirmed(payEvt);

        await Task.Delay(100);

        orders = await orderActor.GetOrders();
        Assert.Single(orders);
        Assert.Equal(OrderStatus.PAYMENT_PROCESSED, orders[0].status);

        /* ⑤ 库存确认：reserved = 1 / available = 99 */
        var s = await stock.GetItem();
        Assert.Equal(1,  s.qty_reserved);
        Assert.Equal(99, s.qty_available);

        /* ⑥ 清理 */
        await orderActor.Reset();
        await stock.Reset();
        await cart.Seal();
    }
}

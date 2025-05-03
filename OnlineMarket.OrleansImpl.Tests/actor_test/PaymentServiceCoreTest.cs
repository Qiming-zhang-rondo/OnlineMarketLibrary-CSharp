// Core.Tests/PaymentServiceCoreTest.cs
using Xunit;
using Moq;
using OnlineMarket.Core.Services;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;
using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class PaymentServiceCoreTest
{
    private InvoiceIssued BuildInvoice()
    {
        var cust = new CustomerCheckout { CustomerId = 1, PaymentType = "CREDIT_CARD",
                                          CardNumber="4111", CardHolderName="Foo",
                                          CardExpiration="12/30", CardBrand="VISA",
                                          Installments=1 };
        var oi = new OrderItem
        {
            order_id = 888,
            seller_id = 77,
            product_id = 99,
            product_name = "Demo",
            unit_price = 10,
            quantity = 1,
            total_items = 10,
            total_amount = 10,
            freight_value = 2
        };
        return new InvoiceIssued(cust, 888, "1-888",
                                 DateTime.UtcNow, 12,
                                 new(){ oi }, Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task PaymentService_Should_Call_All_Ports()
    {
        /*── Arrange : Mock 所有 Port ───────────────────*/
        var stock    = new Mock<IStockGateway>();
        var sellerNt = new Mock<ISellerNotifier>();
        var orderNt  = new Mock<IOrderNotifier>();
        var custNt   = new Mock<ICustomerNotifier>();
        var shipGw   = new Mock<IShipmentGateway>();
        var clock    = new FakeClock();
        var log      = new Mock<ILogger>();

        var svc = new PaymentServiceCore(
                     customerId : 1,
                     stock      : stock.Object,
                     sellerNtfy : sellerNt.Object,
                     orderNtfy  : orderNt.Object,
                     custNtfy   : custNt.Object,
                     ship       : shipGw.Object,
                     clock      : clock,
                     log        : log.Object);

        var inv = BuildInvoice();

        /*── Act ───────────────────────────────────────*/
        await svc.ProcessPaymentAsync(inv);

        /*── Assert : 逐条验证调用 ─────────────────────*/
        // 1. 库存确认
        stock.Verify(s => s.ConfirmAsync(inv.items[0].seller_id,
                                         inv.items[0].product_id,
                                         inv.items[0].quantity),
                     Times.Once);

        // 2. Seller 收到 Invoice & PaymentConfirmed
        sellerNt.Verify(s => s.NotifyInvoiceAsync(inv), Times.Once);
        sellerNt.Verify(s => s.NotifyPaymentConfirmedAsync(
                            It.Is<PaymentConfirmed>(p => p.orderId == inv.orderId)),
                        Times.Once);

        // 3. Order / Customer
        orderNt.Verify(o => o.NotifyPaymentAsync(
                           It.Is<PaymentConfirmed>(p=>p.orderId==inv.orderId)), Times.Once);
        custNt .Verify(c => c.NotifyPaymentAsync(
                           It.Is<PaymentConfirmed>(p=>p.orderId==inv.orderId)), Times.Once);

        // 4. 发货流程启动
        shipGw.Verify(s => s.StartShipmentAsync(
                          It.Is<PaymentConfirmed>(p=>p.orderId==inv.orderId)), Times.Once);
    }
}

/*──── 简单 FakeClock ───*/
file sealed class FakeClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

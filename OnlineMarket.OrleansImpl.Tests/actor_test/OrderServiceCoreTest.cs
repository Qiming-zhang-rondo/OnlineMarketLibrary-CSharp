using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineMarket.Core.Services;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Interfaces;
using Microsoft.Extensions.Logging;

/*──────────── 帮助对象 ────────*/
file sealed class FakeClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }

public class OrderServiceCoreTest
{
    /*— 构造最小  ReserveStock —*/
    private ReserveStock RS(int cid,int oid,int sid)
    {
        var cart = new CartItem{
            SellerId=sid, ProductId=99, ProductName="Demo",
            UnitPrice=10, FreightValue=2, Quantity=1, Voucher=0, Version="1"
        };
        return new(DateTime.UtcNow,
                   new CustomerCheckout{ CustomerId=cid },
                   new(){ cart },
                   Guid.NewGuid().ToString());
    }

    /*— 验证 BuildOrder/Checkout 调用 —*/
    [Fact]
    public async Task Checkout_Should_Call_All_Ports_And_Save()
    {
        /*── 1) Mock 所有 Port ─────────────*/
        var repo   = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetNextIdAsync()).ReturnsAsync(1);

        var stock  = new Mock<IStockReserver>();
        stock.Setup(s => s.TryReserveAsync(It.IsAny<CartItem>()))
             .ReturnsAsync(ItemStatus.IN_STOCK);

        var seller = new Mock<ISellerNotifier>();
        var pay    = new Mock<IPaymentGateway>();
        var clock  = new FakeClock();
        var log    = new Mock<ILogger>();

        var svc = new OrderServiceCore(
                     customerId : 123,
                     repo       : repo.Object,
                     stock      : stock.Object,
                     sellerNtfy : seller.Object,
                     payment    : pay.Object,
                     clock      : clock,
                     log        : log.Object);

        var rs = RS(cid:123, oid:1000, sid:77);

        /*── 2) Act ──────────────────────*/
        await svc.Checkout(rs);

        /*── 3) Assert 保存与调用 ─────────*/
        repo.Verify(r => r.SaveAsync(
                        1,
                        It.Is<Order>(o => o.status==OrderStatus.INVOICED),
                        It.IsAny<List<OrderItem>>(),
                        It.IsAny<List<OrderHistory>>()),
                    Times.Once);

        seller.Verify(s=>s.NotifyInvoiceAsync(It.IsAny<InvoiceIssued>()),Times.Once);
        pay   .Verify(p=>p.StartPaymentAsync(It.IsAny<InvoiceIssued>()),Times.Once);
    }
}

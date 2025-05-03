// OnlineMarket.OrleansImpl.Grains/PaymentActor.cs

using Microsoft.Extensions.DependencyInjection;
using OnlineMarket.Core.Services;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Ports;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans.Concurrency;

[Reentrant]
public sealed class PaymentActor : Grain, IPaymentActor
{
    private IPaymentService _svc = null!;

    public override Task OnActivateAsync(CancellationToken _)
    {
        int cid = (int)this.GetPrimaryKeyLong();
        var cfg = this.ServiceProvider.GetRequiredService<AppConfig>();
        var log = this.ServiceProvider.GetRequiredService<ILogger<PaymentActor>>();

        /* 组装 Ports → Core */
        IStockGateway    stock  = new StockGrainGateway   (GrainFactory);
        ISellerNotifier  sNtfy  = new SellerGrainNotifier (GrainFactory);
        IOrderNotifier   oNtfy  = new OrderGrainNotifier  (GrainFactory);
        ICustomerNotifier cNtfy = new CustomerGrainNotifier(GrainFactory);
        IShipmentGateway ship   = new ShipmentGrainGateway(GrainFactory, cfg);
        IClock           clock  = SystemClock.Instance;

        _svc = new PaymentServiceCore(cid, stock, sNtfy, oNtfy, cNtfy, ship,
            clock, log);

        return Task.CompletedTask;
    }

    /*──────── IPaymentActor 转调 ────────*/
    public Task ProcessPayment(InvoiceIssued inv) =>
        _svc.ProcessPaymentAsync(inv);
}
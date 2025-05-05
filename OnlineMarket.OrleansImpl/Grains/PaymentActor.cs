using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans;
using System.Threading.Tasks;

namespace OnlineMarket.OrleansImpl.Grains
{
    public sealed class PaymentActor : Grain, IPaymentActor
    {
        private PaymentServiceCore paymentService = null!;
        private int customerId;

        private readonly AppConfig config;
        private readonly ILogger<PaymentServiceCore> logger;

        public PaymentActor(AppConfig config, ILogger<PaymentServiceCore> logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            this.customerId = (int)this.GetPrimaryKeyLong();

            paymentService = new PaymentServiceCore(
                logger,
                config,
                getOrderService: id => new OrderActorAdapter(id, GrainFactory),
                getShipmentService: id => new ShipmentActorAdapter(id,GrainFactory),
                getStockService: tuple => new StockActorAdapter(tuple.sellerId, tuple.productId,GrainFactory),
                getSellerService: id => new SellerActorAdapter(id,GrainFactory),
                getCustomerService: id => new CustomerActorAdapter(id, GrainFactory)
            );

            paymentService.SetLogger(async (type, key, value) =>
            {
                var auditLogger = this.ServiceProvider.GetRequiredService<IAuditLogger>();
                await auditLogger.Log(type, key, value);
            });

            return Task.CompletedTask;
        }

        public Task ProcessPayment(InvoiceIssued invoiceIssued)
        {
            return paymentService.ProcessPayment(invoiceIssued);
        }
    }
}
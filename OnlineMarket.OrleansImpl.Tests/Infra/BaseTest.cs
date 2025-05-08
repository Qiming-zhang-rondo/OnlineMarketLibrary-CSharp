using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans.TestingHost;
// using OnlineMarket.OrleansImpl.Transactional;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.OrleansImpl.Infra.SellerDb;

// using OnlineMarket.OrleansImpl.Infra.SellerDb;
// using Microsoft.EntityFrameworkCore;

namespace Test.Infra;

public abstract class BaseTest
{

    protected readonly TestCluster _cluster;
    protected readonly Random random = new Random();

    public BaseTest(TestCluster cluster)
    {
        this._cluster = cluster;
    }
    
    protected SellerDbContext InitSellerDbContext()
    {
        // ① 取 Primary‑Silo 的 IServiceProvider
        var factory = _cluster.ServiceProvider          // ← 就这一行改掉
            .GetRequiredService<IDbContextFactory<SellerDbContext>>();
        
       

        var ctx = factory.CreateDbContext();
        
        Console.WriteLine("→ TestCtx connecting to: " 
                          + ctx.Database.GetDbConnection().ConnectionString);
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        
        
        
        /* ① 如库/表不存在就直接创建 —— 不依赖迁移脚本 */
        ctx.Database.EnsureDeleted();
        // ctx.Database.EnsureCreated();                // ★ 用 EnsureCreated

        
        ctx.Database.Migrate();           // 若已迁移过会自动跳过
        return ctx;
    }

    protected CustomerCheckout BuildCustomerCheckout(int customerId)
    {
        CustomerCheckout customerCheckout = new()
        {
            CustomerId = customerId,
            FirstName = "Customer",
            LastName = "Test",
            Street = "Some unknown street",
            Complement = "Still unknown",
            City = "City of Dreams",
            State = "Orleans",
            ZipCode = "12345",
            PaymentType = PaymentType.CREDIT_CARD.ToString(),
            CardNumber = random.Next().ToString(),
            CardHolderName = "Name",
            CardExpiration = "1224",
            CardSecurityNumber = "001",
            CardBrand = "VISA",
            Installments = 1,
            instanceId = customerId.ToString()
        };
        return customerCheckout;
    }

    protected async Task BuildAndSendCheckout(int customerId = 1, int numItems = 2)
    {
        CustomerCheckout customerCheckout = BuildCustomerCheckout(customerId);

        var cart = _cluster.GrainFactory.GetGrain<ICartActor>(customerId);
        for(int i = 1; i <= numItems; i++){
            await cart.AddItem(GenerateCartItem(1, i));
        }

        await cart.NotifyCheckout(customerCheckout);
    }
    //当前没有实现
    // protected async Task InitData(int numCustomer, int numStockItem)
    // {
    //     // load customer in customer actor
    //     for (var customerId = 1; customerId <= numCustomer; customerId++)
    //     {
    //         var customer = _cluster.GrainFactory.GetGrain<ICustomerActor>(customerId);
    //         await customer.SetCustomer(new Customer()
    //         {
    //             id = customerId,
    //             first_name = "",
    //             last_name = "",
    //             address = "",
    //             complement = "",
    //             birth_date = "",
    //             zip_code = "",
    //             city = "",
    //             state = "",
    //             delivery_count = 0,
    //             failed_payment_count = 0,
    //             success_payment_count = 0
    //         });
    //     }

    //     var config = (AppConfig)_cluster.Client.ServiceProvider.GetService(typeof(AppConfig));

    //     // add correspondent stock items
    //     for (var itemId = 1; itemId <= numStockItem; itemId++)
    //     {
    //         IStockActor stockActor;
    //         if (config.OrleansTransactions)
    //             stockActor = _cluster.GrainFactory.GetGrain<ITransactionalStockActor>(1, itemId.ToString());
    //         else
    //             stockActor = _cluster.GrainFactory.GetGrain<IStockActor>(1, itemId.ToString(), "OrleansApp.Grains.StockActor");

    //         await stockActor.SetItem(new StockItem()
    //         {
    //             product_id = itemId,
    //             seller_id = 1,
    //             qty_available = 10,
    //             qty_reserved = 0,
    //             order_count = 0,
    //             ytd = 1,
    //             version = 1.ToString()
    //         });
    //     }
    // }

    protected CartItem GenerateCartItem(int sellerId, int productId)
    {
        return new()
        {
            ProductId = productId,
            SellerId = sellerId,
            UnitPrice = random.Next(),
            FreightValue = 1,
            Quantity = 1,
            Voucher = 1,
            ProductName = "test",
            Version = 1.ToString()
        };
    }

}



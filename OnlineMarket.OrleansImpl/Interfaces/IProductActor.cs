using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using Orleans.Concurrency;


namespace OnlineMarket.OrleansImpl.Interfaces;

public interface IProductActor : IGrainWithIntegerCompoundKey
{
    Task SetProduct(Product product);

    [ReadOnly]
    Task<Product> GetProduct();

    Task ProcessProductUpdate(Product product);

    Task ProcessPriceUpdate(PriceUpdate priceUpdate);

    Task Reset();
}
using System.Collections.Concurrent;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;

namespace ProductMS.Infractructure
{
    internal sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly ConcurrentDictionary<(int sellerId, int productId), Product> _products = new();

        public Task SaveAsync(Product product)
        {
            _products[(product.seller_id, product.product_id)] = product;
            return Task.CompletedTask;
        }
    }
}
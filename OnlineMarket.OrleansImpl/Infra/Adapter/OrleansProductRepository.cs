// OnlineMarket.OrleansImpl.Infra.Adapter/OrleansProductRepository.cs
using System.Threading.Tasks;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;               // IProductRepository
using Orleans.Runtime;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public sealed class OrleansProductRepository : IProductRepository
    {
        private readonly IPersistentState<Product> _state;
        public OrleansProductRepository(IPersistentState<Product> state) => _state = state;

        public Task SaveAsync(Product product)
        {
            _state.State = product;
            return _state.WriteStateAsync();
        }
    }
}
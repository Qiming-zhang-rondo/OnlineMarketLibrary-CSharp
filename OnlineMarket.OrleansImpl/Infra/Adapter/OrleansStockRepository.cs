// OnlineMarket.OrleansImpl.Adapter/OrleansStockRepository.cs
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Ports;
using Orleans.Runtime;

namespace OnlineMarket.OrleansImpl.Infra.Adapter
{
    public sealed class OrleansStockRepository : IStockRepository
    {
        private readonly IPersistentState<StockItem> _state;
        public OrleansStockRepository(IPersistentState<StockItem> state) => _state = state;

        public Task SaveAsync(StockItem item)
        {
            _state.State = item;
            return _state.WriteStateAsync();
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Events;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Services;
using OnlineMarket.OrleansImpl.Infra.Adapter;
using OnlineMarket.Core.Ports;
using OnlineMarket.OrleansImpl.Infra;
using OnlineMarket.OrleansImpl.Interfaces;
using Orleans;
using Orleans.Runtime;
using Orleans.Concurrency;

namespace OnlineMarket.OrleansImpl.Grains;

[Reentrant]
public sealed class StockActor : Grain, IStockActor
{
    
    private readonly IPersistentState<StockItem> _state;

    private readonly AppConfig _cfg;
    private readonly ILogger<StockServiceCore> _log;
    private StockServiceCore _svc = null!;

    public StockActor(
        [PersistentState("stock", Constants.OrleansStorage)] IPersistentState<StockItem> state,
        AppConfig cfg,
        ILogger<StockServiceCore> log)
    {
        _state = state;
        _cfg   = cfg;
        _log   = log;
    }

    public override Task OnActivateAsync(CancellationToken ct)
    {
        
        int sellerId  = (int)this.GetPrimaryKeyLong(out var prodStr);
        int productId = int.Parse(prodStr);

        //If activated for the first time, initialize State
        if (_state.State is null || _state.State.product_id == 0)
            _state.State = new StockItem { seller_id = sellerId, product_id = productId };
        
        IStockRepository repo = new OrleansStockRepository(_state);
        IClock clock          = SystemClock.Instance;
        
        _svc = new StockServiceCore(
                 sellerId, productId,
                 repo, clock, _log);

        return Task.CompletedTask;
    }

    /*──────── IStockActor Interface ────────*/
    public Task SetItem(StockItem item)                     => _svc.SetItem(item);
    public Task<ItemStatus> AttemptReservation(CartItem c)  => _svc.AttemptReservation(c);
    public Task CancelReservation(int q)                    => _svc.CancelReservation(q);
    public Task ConfirmReservation(int q)                   => _svc.ConfirmReservation(q);
    public Task ProcessProductUpdate(ProductUpdated e)      => _svc.ProcessProductUpdate(e);
    public Task<StockItem> GetItem()                        => _svc.GetItem();
    public Task Reset()                                     => _svc.Reset();
}

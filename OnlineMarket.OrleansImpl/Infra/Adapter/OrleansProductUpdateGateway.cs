namespace OnlineMarket.OrleansImpl.Infra.Adapter;

// OnlineMarket.OrleansImpl.Infra/OrleansProductUpdateGateway.cs
using OnlineMarket.Core.Ports;
using OnlineMarket.Core.Common.Entities;
using Orleans.Streams;

public sealed class OrleansProductUpdateGateway : IProductUpdateGateway
{
    private readonly IStreamProvider _provider;
    private readonly List<StreamSubscriptionHandle<Product>> _handles = new();

    public OrleansProductUpdateGateway(IStreamProvider provider) => _provider = provider;

    public async Task SubscribeAsync(
        int sellerId,
        int productId,
        Func<Product,Task> onChanged)
    {
        var stream = _provider.GetStream<Product>(
            Constants.ProductNameSpace, $"{sellerId}|{productId}");

        /* Use the overload with StreamSequenceToken and ignore the token */
        var handle = await stream.SubscribeAsync(
            (prod, _) => onChanged(prod));

        _handles.Add(handle);
    }

    public async Task UnsubscribeAllAsync()
    {
        foreach (var h in _handles)
            await h.UnsubscribeAsync();
        _handles.Clear();
    }
}


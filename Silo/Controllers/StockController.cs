using System.Net;
using OnlineMarket.Core.Common.Config;
using OnlineMarket.Core.Common.Entities;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.OrleansImpl.Interfaces;

namespace Silo.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class StockController : ControllerBase
{
    private readonly ILogger<StockController> logger;

    public StockController(ILogger<StockController> logger)
    {
        this.logger = logger;
    }

    [HttpPost]
    [Route("/stock")]
    public async Task<ActionResult> AddItem([FromServices] IGrainFactory grains, [FromBody] StockItem item)
    {
        this.logger.LogDebug("[AddItem] received for id {0} {1}", item.seller_id, item.product_id);
        var grain = grains.GetGrain<IStockActor>(item.seller_id, item.product_id.ToString());
        await grain.SetItem(item);
        return Ok();
    }

    [HttpGet("/stock/{sellerId:long}/{productId:long}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(StockItem), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<StockItem>> GetBySellerIdAndProductId([FromServices] IGrainFactory grains, int sellerId, int productId)
    {
        var grain = grains.GetGrain<IStockActor>(sellerId, productId.ToString());
        var item = await grain.GetItem();
        return Ok(item);
    }
}
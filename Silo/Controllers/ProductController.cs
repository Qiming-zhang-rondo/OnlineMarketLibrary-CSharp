using System.Net;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.OrleansImpl.Interfaces;

namespace Silo.Controllers;

[ApiController]
public sealed class ProductController : ControllerBase
{
    private readonly ILogger<ProductController> logger;

    public ProductController(ILogger<ProductController> logger)
    {
        this.logger = logger;
    }

    [HttpPost]
    [Route("/product")]
    public async Task<ActionResult> AddProduct([FromServices] IGrainFactory grains, [FromBody] Product product)
    {
        logger.LogDebug("[AddProduct] received for id {0} {1}", product.seller_id, product.product_id);
        var grain = grains.GetGrain<IProductActor>(product.seller_id, product.product_id.ToString());
        await grain.SetProduct(product);
        return Ok();
    }

    [HttpGet("/product/{sellerId:long}/{productId:long}")]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(Product), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<Product>> GetBySellerIdAndProductId([FromServices] IGrainFactory grains, int sellerId, int productId)
    {
        var grain = grains.GetGrain<IProductActor>(sellerId, productId.ToString());
        var product = await grain.GetProduct();
        if (product is null)
            return NotFound();
        return Ok(product);
    }

    [HttpPatch]
    [Route("/product")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult> ProcessPriceUpdate([FromServices] IGrainFactory grains, [FromBody] PriceUpdate update)
    {
        var grain = grains.GetGrain<IProductActor>(update.sellerId, update.productId.ToString());
        try
        {
            await grain.ProcessPriceUpdate(update);
            return Accepted();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during ProcessPriceUpdate for sellerId {0}, productId {1}", update.sellerId, update.productId);
            return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
        }
    }

    [HttpPut]
    [Route("/product")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult> ProcessUpdateProduct([FromServices] IGrainFactory grains, [FromBody] Product product)
    {
        var grain = grains.GetGrain<IProductActor>(product.seller_id, product.product_id.ToString());
        try
        {
            await grain.ProcessProductUpdate(product);
            return Accepted();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during ProcessProductUpdate for sellerId {0}, productId {1}", product.seller_id, product.product_id);
            return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
        }
    }
}
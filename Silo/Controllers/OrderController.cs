using System.Net;
using OnlineMarket.Core.Common.Config;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.OrleansImpl.Interfaces; 

namespace Silo.Controllers;

[ApiController]
public sealed class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> logger;

    public OrderController(ILogger<OrderController> logger)
    {
        this.logger = logger;
    }

    [HttpGet("/order/{customerId}")]
    [ProducesResponseType(typeof(IEnumerable<OnlineMarket.Core.Common.Entities.Order>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<IEnumerable<OnlineMarket.Core.Common.Entities.Order>>> GetByCustomerId(
        [FromServices] IGrainFactory grains,
        int customerId)
    {
        var orderActor = grains.GetGrain<IOrderActor>(customerId);
        var orders = await orderActor.GetOrders();
        return Ok(orders);
    }
}
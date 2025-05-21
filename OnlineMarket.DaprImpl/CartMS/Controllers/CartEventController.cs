// DaprImpl/CartMS/Controllers/EventController.cs

using Dapr;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.Common.Events;
using CartMS.Services;
using OnlineMarket.Core.Common.Requests;

namespace CartMS.Controllers;

[ApiController]
[Route("cart/events")]
public sealed class EventController : ControllerBase
{
    private readonly CartService _svc;
    private readonly ILogger<EventController> _log;
    private const string PUBSUB_NAME = "pubsub";

    public EventController(CartService svc, ILogger<EventController> log)
    {
        _svc = svc;
        _log = log;
    }

    [HttpPost("product-updated")]
    [Topic(PUBSUB_NAME, nameof(ProductUpdated))]
    public async Task<IActionResult> ProcessProductUpdated([FromBody] ProductUpdated evt)
    {
        try
        {
            await _svc.HandleProductUpdatedAsync(evt);
            return Ok();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to process ProductUpdated event");
            await _svc.HandlePoisonProductUpdatedAsync(evt);
            return StatusCode(500);
        }
    }

    [HttpPost("price-updated")]
    [Topic(PUBSUB_NAME, nameof(PriceUpdate))]
    public async Task<IActionResult> ProcessPriceUpdated([FromBody] PriceUpdate evt)
    {
        try
        {
            await _svc.HandlePriceUpdatedAsync(evt);
            return Ok();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to process PriceUpdated event");
            await _svc.HandlePoisonPriceUpdatedAsync(evt);
            return StatusCode(500);
        }
    }
}
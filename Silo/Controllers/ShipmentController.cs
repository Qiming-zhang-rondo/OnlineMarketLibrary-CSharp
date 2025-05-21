using System.Net;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.OrleansImpl.Interfaces;
using Microsoft.Extensions.Logging;

namespace Silo.Controllers;

[ApiController]
public sealed class ShipmentController : ControllerBase
{
    private readonly ILogger<ShipmentController> logger;

    public ShipmentController(ILogger<ShipmentController> logger)
    {
        this.logger = logger;
    }

    [HttpPatch]
    [Route("/shipment/{instanceId}")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult> UpdateShipment([FromServices] IGrainFactory grains, string instanceId)
    {
        try
        {
            // 这里我们用 instanceId 做 grain ID，也可以换成其他 ID 方案
            var shipmentActor = grains.GetGrain<IShipmentActor>(0);  // 这里的 0 可以换成更合理的 key
            await shipmentActor.UpdateShipment(instanceId);
            return Accepted();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating shipment with instanceId {InstanceId}", instanceId);
            return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
        }
    }
}
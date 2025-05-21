using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using OnlineMarket.Core.Interfaces;

namespace ProductMS.Controllers
{
    [ApiController]
    [Route("product")]
    public sealed class ProductController : ControllerBase
    {
        private readonly IProductService _svc;
        private readonly ILogger<ProductController> _log;

        public ProductController(IProductService svc, ILogger<ProductController> log)
        {
            _svc = svc;
            _log = log;
        }

        [HttpGet]
        public async Task<ActionResult<Product>> Get()
            => Ok(await _svc.GetProduct());

        [HttpPost("set")]
        public async Task<IActionResult> Set([FromBody] Product product)
        {
            try
            {
                await _svc.SetProduct(product);
                return Ok();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "SetProduct failed");
                return StatusCode(500);
            }
        }

        [HttpPatch("update")]
        public async Task<IActionResult> Update([FromBody] Product product)
        {
            try
            {
                await _svc.ProcessProductUpdate(product);
                return Ok();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "UpdateProduct failed");
                return StatusCode(500);
            }
        }

        [HttpPatch("price")]
        public async Task<IActionResult> UpdatePrice([FromBody] PriceUpdate update)
        {
            try
            {
                await _svc.ProcessPriceUpdate(update);
                return Ok();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "UpdatePrice failed");
                return StatusCode(500);
            }
        }

        [HttpPatch("reset")]
        public async Task<IActionResult> Reset()
        {
            await _svc.Reset();
            return Ok();
        }
    }
}
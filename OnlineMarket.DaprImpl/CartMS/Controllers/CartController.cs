// Controllers/CartController.cs
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.Core.Interfaces;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CartMS.Controllers;

[ApiController]
[Route("cart")]
public sealed class CartController : ControllerBase
{
    private readonly ICartService _svc;
    private readonly ILogger<CartController> _log;

    public CartController(ICartService svc, ILogger<CartController> log)
    {
        _svc = svc; _log = log;
    }

    [HttpGet("{cid:int}")]
    public async Task<ActionResult<Cart>> Get(int cid)
        => Ok(await _svc.GetCartAsync());

    [HttpPatch("{cid:int}/add")]
    public async Task<IActionResult> Add(int cid, [FromBody] CartItem item)
    {
        // 如果 CartItem 有 customerId 字段，就加一行确保一致：
        // item.CustomerId = cid;
        await _svc.AddItemAsync(item);
        return Accepted();
    }

    [HttpPost("{cid:int}/checkout")]
public async Task<IActionResult> Checkout(int cid, [FromBody] CustomerCheckout co)
{
    _log.LogWarning("==> Entered Checkout action, cid: {cid}", cid);
    // 1️⃣ 打印进来了没：
    _log.LogInformation("Checkout called with CustomerId: {CustomerId}, InstanceId: {InstanceId}", co?.CustomerId, co?.instanceId);

    // 2️⃣ 检查模型是否有效：
    if (!ModelState.IsValid)
    {
        _log.LogWarning("Invalid model state: {@ModelState}", ModelState);
        return BadRequest(ModelState);
    }

    // 3️⃣ 尝试执行
    try
    {
        await _svc.NotifyCheckoutAsync(co);
        return Accepted();
    }
    catch (Exception ex)
    {
        _log.LogError(ex, "Failed to process checkout for CustomerId: {CustomerId}", co?.CustomerId);
        return StatusCode(500, "Internal error occurred");
    }
}

    [HttpPatch("{cid:int}/seal")]
    public Task<IActionResult> Seal(int cid)
        => _svc.SealAsync().ContinueWith(_ => Accepted() as IActionResult);

    [HttpPatch("reset")]
    public Task<IActionResult> Reset()
        => _svc.ResetAsync().ContinueWith(_ => Ok() as IActionResult);



        [HttpGet("ping")]
public IActionResult Ping() => Ok("pong");
}
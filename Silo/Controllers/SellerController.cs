﻿using System.Net;
using OnlineMarket.Core.Common.Entities;
using Microsoft.AspNetCore.Mvc;
using OnlineMarket.OrleansImpl.Interfaces;
using OnlineMarket.Core.Common.Integration;

namespace Silo.Controllers;

[ApiController]
public sealed class SellerController : ControllerBase
{
    private readonly ILogger<SellerController> logger;

    public SellerController(ILogger<SellerController> logger)
    {
        this.logger = logger;
    }

    [HttpPost]
    [Route("/seller")]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    public async Task<ActionResult> SetSeller([FromServices] IGrainFactory grains, [FromBody] Seller seller)
    {
        logger.LogDebug("[SetSeller] received for id {0}", seller.id);
        var actor = grains.GetGrain<ISellerActor>(seller.id);
        await actor.SetSeller(seller);
        return StatusCode((int)HttpStatusCode.Created);
    }

    [HttpGet]
    [Route("/seller/dashboard/{sellerId}")]
    [ProducesResponseType(typeof(SellerDashboard), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<SellerDashboard>> GetDashboard([FromServices] IGrainFactory grains, int sellerId)
    {
        var actor = grains.GetGrain<ISellerActor>(sellerId);
        var dash = await actor.QueryDashboard();
        return Ok(dash);
    }

    [HttpGet]
    [Route("/seller/{sellerId}")]
    [ProducesResponseType(typeof(Seller), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<Seller>> GetSeller([FromServices] IGrainFactory grains, int sellerId)
    {
        var actor = grains.GetGrain<ISellerActor>(sellerId);
        var seller = await actor.GetSeller();
        return Ok(seller);
    }
}
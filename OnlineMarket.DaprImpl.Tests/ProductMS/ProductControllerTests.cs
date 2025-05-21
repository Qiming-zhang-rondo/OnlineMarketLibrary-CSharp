using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using OnlineMarket.Core.Common.Entities;
using OnlineMarket.Core.Common.Requests;
using ProductMS;  

namespace OnlineMarket.DaprImpl.Tests.ProductMS;

public class ProductControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddProduct_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var product = new Product
        {
            seller_id = 1,
            product_id = 1001,
            name = "Test Product",
            sku = "SKU-1001",
            category = "Electronics",
            description = "Test product description.",
            price = 199.99f,
            freight_value = 9.99f,
            status = "Available",
            version = "v1"
        };

        // Act
        var response = await client.PostAsJsonAsync("/product/set", product);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var product = new Product
        {
            seller_id = 1,
            product_id = 1001,
            name = "Updated Product",
            sku = "SKU-1001",
            category = "Electronics",
            description = "Updated description.",
            price = 179.99f,
            freight_value = 8.99f,
            status = "Available",
            version = "v2"
        };

        // Act
        var response = await client.PatchAsJsonAsync("/product/update", product);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProductPrice_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var priceUpdate = new PriceUpdate
        {
            sellerId = 1,
            productId = 1001,
            price = 149.99f,
            instanceId = "12345"
        };

        // Act
        var response = await client.PatchAsJsonAsync("/product/price", priceUpdate);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProduct_ShouldReturnOkOrNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/product");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task Reset_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PatchAsync("/product/reset", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
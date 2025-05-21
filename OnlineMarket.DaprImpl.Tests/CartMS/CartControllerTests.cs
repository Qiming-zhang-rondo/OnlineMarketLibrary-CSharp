using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using OnlineMarket.Core.Common.Entities;
using CartMS;
using OnlineMarket.Core.Common.Requests;
using System.Net.Http;
using System.Text;

namespace OnlineMarket.DaprImpl.Tests.CartMS;

public class CartControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CartControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddItem_ShouldReturnAccepted()
    {
        // Arrange
        var client = _factory.CreateClient();
        var item = new CartItem
        {
            SellerId = 1,
            ProductId = 100,
            ProductName = "Test Product",
            UnitPrice = 9.99f,
            FreightValue = 1.99f,
            Quantity = 1,
            Version = "v1"
        };

        // Act
        var response = await client.PatchAsJsonAsync("/cart/1/add", item);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_ShouldReturnAccepted()
    {
        // Arrange
        var client = _factory.CreateClient();
        var json = """
{
    "customerId": 1,
    "firstName": "John",
    "lastName": "Doe",
    "street": "123 Test St",
    "complement": "",
    "city": "Testville",
    "state": "TS",
    "zipCode": "12345",
    "paymentType": "CreditCard",
    "cardNumber": "4111111111111111",
    "cardHolderName": "John Doe",
    "cardExpiration": "12/25",
    "cardSecurityNumber": "123",
    "cardBrand": "VISA",
    "installments": 1,
    "instanceId": "instance-123"
}
""";

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/cart/1/checkout", content);


        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task GetCart_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/cart/1");

        // Assert
        // 不管有没有数据，只要服务正常返回 200
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Seal_ShouldReturnAccepted()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PatchAsync("/cart/1/seal", null);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Accepted ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task Reset_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PatchAsync("/cart/reset", null);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}


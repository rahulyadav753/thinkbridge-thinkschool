using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using RefactorTask.Dtos;
using Xunit;

namespace RefactorTask.Tests;

public class OrderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreatedResponse()
    {
        var client = _factory.CreateClient();
        var request = new OrderCreateRequest
        {
            CustomerId = 1,
            CustomerName = "Integration Test",
            Items = new() { new OrderItemDto { ProductId = 1, Quantity = 1 } }
        };

        var response = await client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }
}

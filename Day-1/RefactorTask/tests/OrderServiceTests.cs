using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using RefactorTask.Dtos;
using RefactorTask.Models;
using RefactorTask.Repositories;
using RefactorTask.Services;
using Xunit;

namespace RefactorTask.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_Succeeds_WithValidOrderAndCorrectItemCount()
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(r => r.GetCustomerByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer { Id = 1, Name = "Test", Email = "test@example.com" });
        repository.Setup(r => r.GetProductByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = 1, Name = "Widget", Price = 10m, IsDiscontinued = false });
        repository.Setup(r => r.GetRecentOrderCountAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        repository.Setup(r => r.AddOrderAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.AddOrderItemsAsync(It.IsAny<IEnumerable<OrderItem>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<OrderService>>();
        var service = new OrderService(repository.Object, logger.Object);

        var request = new OrderCreateRequest
        {
            CustomerId = 1,
            CustomerName = "Jane Doe",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };

        var response = await service.CreateOrderAsync(request, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(1, response.ItemCount);
        Assert.Equal(1, response.CustomerId);
        Assert.Equal("Review", response.Status);
    }

    [Fact]
    public async Task CreateOrderAsync_ThrowsValidationException_ForInvalidRequest()
    {
        var repository = new Mock<IOrderRepository>();
        var logger = new Mock<ILogger<OrderService>>();
        var service = new OrderService(repository.Object, logger.Object);

        var request = new OrderCreateRequest
        {
            CustomerId = 0,
            CustomerName = string.Empty,
            Items = new List<OrderItemDto>()
        };

        await Assert.ThrowsAsync<OrderValidationException>(() => service.CreateOrderAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateOrderAsync_ThrowsEntityNotFoundException_WhenCustomerMissing()
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(r => r.GetCustomerByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var logger = new Mock<ILogger<OrderService>>();
        var service = new OrderService(repository.Object, logger.Object);

        var request = new OrderCreateRequest
        {
            CustomerId = 42,
            CustomerName = "Missing Customer",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.CreateOrderAsync(request, CancellationToken.None));
    }
}

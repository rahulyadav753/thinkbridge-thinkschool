using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RefactorTask.Dtos;
using RefactorTask.Models;
using RefactorTask.Repositories;

namespace RefactorTask.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(OrderCreateRequest request, CancellationToken cancellationToken);
}

public class OrderService : IOrderService
{
    private const int MaxItemsPerOrder = 50;
    private const decimal BulkDiscountThreshold = 10m;
    private const decimal BulkDiscountRate = 0.05m;
    private const decimal WelcomeDiscountAmount = 10m;
    private const decimal LargeOrderThreshold = 200m;
    private const decimal HighPriorityThreshold = 500m;
    private const decimal MinimumOrderTotal = 20m;
    private const string BlackFridayCode = "BLACKFRIDAY";
    private const string WelcomeCode = "WELCOME";

    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateOrderAsync(OrderCreateRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new OrderValidationException("Order request is required.");
        }

        if (request.CustomerId <= 0)
        {
            throw new OrderValidationException("CustomerId must be greater than zero.");
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            throw new OrderValidationException("Order must contain at least one item.");
        }

        if (request.Items.Count > MaxItemsPerOrder)
        {
            throw new OrderValidationException($"Order cannot contain more than {MaxItemsPerOrder} items.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new OrderValidationException("CustomerName is required.");
        }

        var customer = await _repository.GetCustomerByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
        {
            throw new EntityNotFoundException($"Customer with ID {request.CustomerId} was not found.");
        }

        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0m;

        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];
            if (item == null)
            {
                throw new OrderValidationException($"Order item at position {index} is required.");
            }

            if (item.Quantity <= 0)
            {
                throw new OrderValidationException($"Quantity for item {index} must be greater than zero.");
            }

            var product = await _repository.GetProductByIdAsync(item.ProductId, cancellationToken);
            if (product == null)
            {
                throw new EntityNotFoundException($"Product with ID {item.ProductId} was not found.");
            }

            if (product.IsDiscontinued)
            {
                throw new OrderValidationException($"Product '{product.Name}' is discontinued.");
            }

            decimal unitPrice = product.Price;
            if (item.Quantity > BulkDiscountThreshold)
            {
                unitPrice *= 1 - BulkDiscountRate;
            }

            if (!string.IsNullOrWhiteSpace(request.DiscountCode) &&
                request.DiscountCode.Trim().Equals(BlackFridayCode, StringComparison.OrdinalIgnoreCase))
            {
                unitPrice *= 0.80m;
            }

            var lineTotal = unitPrice * item.Quantity;
            if (lineTotal < 0)
            {
                throw new OrderValidationException("Line total must not be negative.");
            }

            totalAmount += lineTotal;
            orderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                TotalPrice = lineTotal,
                ProductName = product.Name
            });
        }

        if (!string.IsNullOrWhiteSpace(request.DiscountCode) &&
            request.DiscountCode.Trim().Equals(WelcomeCode, StringComparison.OrdinalIgnoreCase))
        {
            totalAmount -= WelcomeDiscountAmount;
        }

        if (totalAmount < 0)
        {
            totalAmount = 0;
        }

        if (totalAmount > LargeOrderThreshold)
        {
            totalAmount -= 50;
        }

        var status = totalAmount > LargeOrderThreshold ? "Ready" : "Review";
        var priority = totalAmount > HighPriorityThreshold ? "High" : "Normal";

        var order = new Order
        {
            CustomerId = request.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = totalAmount,
            Status = status,
            Priority = priority,
            Notes = request.Notes,
            Items = orderItems
        };

        var previousOrderCount = await _repository.GetRecentOrderCountAsync(request.CustomerId, cancellationToken);

        if (previousOrderCount > 0 && totalAmount < MinimumOrderTotal)
        {
            throw new OrderValidationException($"Order total must be at least {MinimumOrderTotal} for returning customers.");
        }

        await _repository.AddOrderAsync(order, cancellationToken);
        await _repository.AddOrderItemsAsync(orderItems, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created order {OrderId} for customer {CustomerId} with {ItemCount} items.", order.Id, order.CustomerId, order.Items.Count);

        return new OrderResponse(order.Id, order.CustomerId, order.TotalAmount, order.Status, order.Priority, order.Items.Count);
    }
}

public class OrderValidationException : Exception
{
    public OrderValidationException(string message)
        : base(message)
    {
    }
}

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message)
        : base(message)
    {
    }
}

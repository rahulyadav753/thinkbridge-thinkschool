using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RefactorTask.Dtos;
using RefactorTask.Models;
using RefactorTask.Repositories;
using RefactorTask.Services.Rules;

namespace RefactorTask.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(OrderCreateRequest request, CancellationToken cancellationToken);
}

public class OrderService : IOrderService
{
    private const int MaxItemsPerOrder = 50;

    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;
    private readonly IEnumerable<IOrderRule> _orderRules;

    public OrderService(IOrderRepository repository, ILogger<OrderService> logger, IEnumerable<IOrderRule> orderRules)
    {
        _repository = repository;
        _logger = logger;
        _orderRules = orderRules;
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

            orderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * item.Quantity,
                ProductName = product.Name
            });
        }

        var previousOrderCount = await _repository.GetRecentOrderCountAsync(request.CustomerId, cancellationToken);
        var context = new OrderRuleContext(request, orderItems, orderItems.Sum(item => item.TotalPrice))
        {
            PreviousOrderCount = previousOrderCount
        };

        foreach (var rule in _orderRules)
        {
            rule.Apply(context);
        }

        if (context.TotalAmount < 0)
        {
            context.TotalAmount = 0;
        }

        var order = new Order
        {
            CustomerId = request.CustomerId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = context.TotalAmount,
            Status = context.Status,
            Priority = context.Priority,
            Notes = request.Notes,
            Items = orderItems
        };

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

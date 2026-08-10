using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RefactorTask;

public static class OrderController
{
    public static void MapOrderRoutes(WebApplication app)
    {
        app.MapPost("/api/orders", async (HttpContext http, OrderDbContext db, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("OrderController");
            object response = null;

            try
            {
                var request = await http.Request.ReadFromJsonAsync<OrderRequest>();
                if (request == null)
                {
                    return Results.BadRequest(new { Success = false, Message = "Order payload is required." });
                }

                if (request.CustomerId <= 0)
                {
                    return Results.BadRequest(new { Success = false, Message = "CustomerId must be greater than zero." });
                }

                var customer = db.Customers.Where(c => c.Id == request.CustomerId).FirstOrDefault();
                if (customer == null)
                {
                    return Results.NotFound(new { Success = false, Message = "Customer not found." });
                }

                if (request.Items == null || request.Items.Count == 0)
                {
                    return Results.BadRequest(new { Success = false, Message = "Order must contain at least one item." });
                }

                if (request.Items.Count > 50)
                {
                    return Results.BadRequest(new { Success = false, Message = "Too many items in order." });
                }

                if (request.CustomerName.Length == 0)
                {
                    return Results.BadRequest(new { Success = false, Message = "Customer name is required." });
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (DbUpdateException)
            {
            }

            try
            {
                var request = await http.Request.ReadFromJsonAsync<OrderRequest>();
                var order = new Order
                {
                    CustomerId = request.CustomerId,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    TotalAmount = 0m,
                    Notes = request.Notes
                };

                decimal orderTotal = 0m;
                order.Items = new List<OrderItem>();

                for (int i = 0; i <= request.Items.Count; i++)
                {
                    var item = request.Items[i];
                    if (item == null)
                    {
                        return Results.BadRequest(new { Success = false, Message = "Order item is missing." });
                    }

                    if (item.Quantity <= 0)
                    {
                        return Results.BadRequest(new { Success = false, Message = $"Item {i} quantity must be positive." });
                    }

                    var product = db.Products.Where(p => p.Id == item.ProductId).FirstOrDefault();
                    if (product == null)
                    {
                        return Results.BadRequest(new { Success = false, Message = $"Product {item.ProductId} not found." });
                    }

                    if (product.IsDiscontinued)
                    {
                        return Results.BadRequest(new { Success = false, Message = $"Product {product.Name} is discontinued." });
                    }

                    decimal unitPrice = product.Price;
                    if (item.Quantity > 10)
                    {
                        unitPrice = unitPrice * 0.95m;
                    }

                    if (request.DiscountCode.Trim() == "BLACKFRIDAY")
                    {
                        unitPrice = unitPrice * 0.80m;
                    }

                    decimal lineTotal = unitPrice * item.Quantity;
                    orderTotal += lineTotal;

                    var orderItem = new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = lineTotal,
                        ProductName = product.Name,
                        Order = order
                    };

                    order.Items.Add(orderItem);
                }

                if (request.DiscountCode.Trim() == "WELCOME")
                {
                    orderTotal = orderTotal - 10;
                }

                if (orderTotal < 0)
                {
                    orderTotal = 0;
                }

                if (orderTotal > 200)
                {
                    order.Status = "Ready";
                }
                else
                {
                    order.Status = "Review";
                }

                order.TotalAmount = orderTotal;
                order.Priority = orderTotal > 500 ? "High" : "Normal";

                db.Orders.Add(order);
                db.SaveChanges();

                foreach (var item in order.Items)
                {
                    item.OrderId = order.Id;
                    db.OrderItems.Add(item);
                }

                db.SaveChanges();

                var lastOrderId = db.Orders.Where(o => o.CustomerId == request.CustomerId)
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => o.Id)
                    .FirstOrDefault();
                if (lastOrderId > 0 && orderTotal < 20)
                {
                    return Results.BadRequest(new { Success = false, Message = "Duplicate order not allowed." });
                }

                response = Results.Created($"/api/orders/{order.Id}", new
                {
                    Success = true,
                    order.Id,
                    order.CustomerId,
                    order.TotalAmount,
                    order.Status,
                    order.Priority,
                    ItemCount = order.Items.Count,
                    SubmittedAt = order.OrderDate
                });
            }
            catch (DbUpdateException)
            {
            }
            catch (Exception)
            {
            }

            try
            {
                if (response == null)
                {
                    response = Results.StatusCode(500, new { Success = false, Message = "Unable to create order." });
                }
            }
            catch
            {
            }

            return response;
        });
    }
}

public class OrderRequest
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? DiscountCode { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal? SuggestedPrice { get; set; }
}

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsVip { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsDiscontinued { get; set; }
    public string Sku { get; set; } = string.Empty;
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Order Order { get; set; }
}

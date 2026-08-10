using Microsoft.EntityFrameworkCore;
using RefactorTask.Data;
using RefactorTask.Repositories;
using RefactorTask.Services;
using RefactorTask.Services.Rules;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("OrdersDb"));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderRule, BulkDiscountRule>();
builder.Services.AddScoped<IOrderRule, BlackFridayDiscountRule>();
builder.Services.AddScoped<IOrderRule, WelcomeDiscountRule>();
builder.Services.AddScoped<IOrderRule, LargeOrderDiscountRule>();
builder.Services.AddScoped<IOrderRule, OrderStatusRule>();
builder.Services.AddScoped<IOrderRule, OrderPriorityRule>();
builder.Services.AddScoped<IOrderRule, ReturningCustomerMinimumOrderRule>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Customers.Any())
    {
        db.Customers.Add(new RefactorTask.Models.Customer { Id = 1, Name = "Test Customer", Email = "test@example.com", Phone = "000-000-0000" });
    }

    if (!db.Products.Any())
    {
        db.Products.Add(new RefactorTask.Models.Product { Id = 1, Name = "Test Product", Price = 10m, IsDiscontinued = false, Sku = "TEST-1" });
    }

    db.SaveChanges();
}

app.MapControllers();

app.Run();

public partial class Program { }

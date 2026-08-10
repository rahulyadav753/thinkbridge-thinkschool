using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RefactorTask.Data;
using RefactorTask.Models;

namespace RefactorTask.Repositories;

public interface IOrderRepository
{
    Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken);
    Task<Product?> GetProductByIdAsync(int productId, CancellationToken cancellationToken);
    Task AddOrderAsync(Order order, CancellationToken cancellationToken);
    Task AddOrderItemsAsync(IEnumerable<OrderItem> items, CancellationToken cancellationToken);
    Task<int> GetRecentOrderCountAsync(int customerId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken)
        => _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

    public Task<Product?> GetProductByIdAsync(int productId, CancellationToken cancellationToken)
        => _context.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

    public async Task AddOrderAsync(Order order, CancellationToken cancellationToken)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public async Task AddOrderItemsAsync(IEnumerable<OrderItem> items, CancellationToken cancellationToken)
    {
        await _context.OrderItems.AddRangeAsync(items, cancellationToken);
    }

    public Task<int> GetRecentOrderCountAsync(int customerId, CancellationToken cancellationToken)
        => _context.Orders.Where(o => o.CustomerId == customerId).CountAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}

using System.Linq;
using System.Collections.Generic;
using RefactorTask.Dtos;
using RefactorTask.Models;

namespace RefactorTask.Services.Rules;

public sealed class OrderRuleContext
{
    public OrderCreateRequest Request { get; }
    public List<OrderItem> OrderItems { get; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int PreviousOrderCount { get; set; }

    public OrderRuleContext(OrderCreateRequest request, List<OrderItem> orderItems, decimal totalAmount)
    {
        Request = request;
        OrderItems = orderItems;
        TotalAmount = totalAmount;
    }

    public void RecalculateTotal()
    {
        TotalAmount = OrderItems.Sum(item => item.TotalPrice);
    }
}

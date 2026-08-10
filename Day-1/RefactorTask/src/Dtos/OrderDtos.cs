using System.Collections.Generic;

namespace RefactorTask.Dtos;

public class OrderCreateRequest
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? DiscountCode { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal? SuggestedPrice { get; set; }
}

public sealed record OrderResponse(int OrderId, int CustomerId, decimal TotalAmount, string Status, string Priority, int ItemCount);

public sealed record ErrorResponse(string ErrorMessage);

namespace RefactorTask.Services.Rules;

public class BulkDiscountRule : IOrderRule
{
    private const int Threshold = 10;
    private const decimal DiscountRate = 0.05m;

    public void Apply(OrderRuleContext context)
    {
        foreach (var item in context.OrderItems)
        {
            var lineTotal = item.UnitPrice * item.Quantity;
            if (item.Quantity > Threshold)
            {
                lineTotal *= 1 - DiscountRate;
            }

            item.TotalPrice = lineTotal;
        }

        context.RecalculateTotal();
    }
}

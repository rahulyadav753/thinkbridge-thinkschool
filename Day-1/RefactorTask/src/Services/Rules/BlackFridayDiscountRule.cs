using System;
using RefactorTask.Services;

namespace RefactorTask.Services.Rules;

public class BlackFridayDiscountRule : IOrderRule
{
    private const string DiscountCode = "BLACKFRIDAY";
    private const decimal DiscountRate = 0.20m;

    public void Apply(OrderRuleContext context)
    {
        if (!string.Equals(context.Request.DiscountCode?.Trim(), DiscountCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var item in context.OrderItems)
        {
            item.TotalPrice *= 1 - DiscountRate;
        }

        context.RecalculateTotal();
    }
}

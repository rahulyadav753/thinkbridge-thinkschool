using System;
using RefactorTask.Services;

namespace RefactorTask.Services.Rules;

public class WelcomeDiscountRule : IOrderRule
{
    private const string DiscountCode = "WELCOME";
    private const decimal DiscountAmount = 10m;

    public void Apply(OrderRuleContext context)
    {
        if (!string.Equals(context.Request.DiscountCode?.Trim(), DiscountCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        context.TotalAmount -= DiscountAmount;
    }
}

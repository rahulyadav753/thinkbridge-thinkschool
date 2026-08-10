using RefactorTask.Services;

namespace RefactorTask.Services.Rules;

public class ReturningCustomerMinimumOrderRule : IOrderRule
{
    private const decimal MinimumOrderTotal = 20m;

    public void Apply(OrderRuleContext context)
    {
        if (context.PreviousOrderCount > 0 && context.TotalAmount < MinimumOrderTotal)
        {
            throw new OrderValidationException($"Order total must be at least {MinimumOrderTotal} for returning customers.");
        }
    }
}

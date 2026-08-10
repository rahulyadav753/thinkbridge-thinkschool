namespace RefactorTask.Services.Rules;

public class LargeOrderDiscountRule : IOrderRule
{
    private const decimal Threshold = 200m;
    private const decimal DiscountAmount = 50m;

    public void Apply(OrderRuleContext context)
    {
        if (context.TotalAmount > Threshold)
        {
            context.TotalAmount -= DiscountAmount;
        }
    }
}

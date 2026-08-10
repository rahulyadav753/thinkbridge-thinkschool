namespace RefactorTask.Services.Rules;

public class OrderStatusRule : IOrderRule
{
    private const decimal ReadyThreshold = 200m;

    public void Apply(OrderRuleContext context)
    {
        context.Status = context.TotalAmount > ReadyThreshold ? "Ready" : "Review";
    }
}

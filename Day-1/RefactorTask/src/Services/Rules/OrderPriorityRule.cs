namespace RefactorTask.Services.Rules;

public class OrderPriorityRule : IOrderRule
{
    private const decimal HighPriorityThreshold = 500m;

    public void Apply(OrderRuleContext context)
    {
        context.Priority = context.TotalAmount > HighPriorityThreshold ? "High" : "Normal";
    }
}

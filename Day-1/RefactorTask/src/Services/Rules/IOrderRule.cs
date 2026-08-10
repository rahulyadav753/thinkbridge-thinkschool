namespace RefactorTask.Services.Rules;

public interface IOrderRule
{
    void Apply(OrderRuleContext context);
}

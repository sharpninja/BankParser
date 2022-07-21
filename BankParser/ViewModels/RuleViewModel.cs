namespace BankParser.ViewModels;

using Core.Models.Rules;

using Syncfusion.UI.Xaml.Data;

[ObservableObject]
public partial class RuleViewModel
{
    public string RuleName
    {
        get;
    }

    public NotifyingList<IRule> Rules
    {
        get;
    } = new();

    public NotifyingList<RuleAction> RuleActions
    {
        get;
    } = new();


    public RuleViewModel(IRule rule, string? ruleName = null)
    {
        RuleName = ruleName ?? $"{rule.GetType().Name} Rule";
        Rules.Add(rule);
    }

    public void ApplyRuleActions(
        BankTransactionView trx,
        params object[] parameters
    )
        => RuleActions.ForEach(action => action.ApplyRuleAction(this, trx, parameters));
}

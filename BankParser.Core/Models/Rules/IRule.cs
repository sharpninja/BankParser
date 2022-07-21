namespace BankParser.Core.Models.Rules;

public interface IRule
{
    RuleTypes RuleType { get; }
    Action<BankTransactionView> Action { get; }
    void ApplyRule(IEnumerable<BankTransactionView> transactions);
}
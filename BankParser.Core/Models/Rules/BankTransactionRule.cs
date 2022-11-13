using System;

using static BankParser.Core.Models.Rules.BankTransactionRuleResolver;
// ReSharper disable UnusedMember.Global

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record struct BankTransactionRule<TDelegate>(
    RuleTypes RuleType,
    BankTransactionRuleResolver<TDelegate> SelectionResolver,
    Action<BankTransactionView> Action
) : IRule
    where TDelegate : Delegate
{
    public readonly string RuleName => RuleType.ToString();

    private readonly bool InvokeResolver(BankTransactionView trx)
        => (bool?)SelectionResolver.Predicate.DynamicInvoke(trx) ?? false;

    public void ApplyRule(IEnumerable<BankTransactionView> transactions)
    {
        foreach (BankTransactionView trx in transactions
            .Where(InvokeResolver))
        {
            Action(trx);
        }
    }
}

public record struct BankTransactionRule
{
    public static Dictionary<RuleTypes, BankTransactionRuleResolver<Delegate>> AvailableRules
        => new()
            {
                { RuleTypes.IsEqualToRule, new(GetMathHandler(Comparisons.IsEqualTo)) },
                { RuleTypes.IsLessThanRule, new (GetMathHandler(Comparisons.IsLessThan)) },
                { RuleTypes.IsLessThanOrEqualToRule, new (GetMathHandler(Comparisons.IsLessThanOrEqualTo)) },
                { RuleTypes.IsGreaterThanRule, new (GetMathHandler(Comparisons.IsGreaterThan)) },
                { RuleTypes.IsGreaterThanOrEqualToRule, new (GetMathHandler(Comparisons.IsGreaterThanOrEqualTo)) },
                { RuleTypes.PatternRule, new (GetPredicate(RuleTypes.PatternRule)) },
                { RuleTypes.RegexRule, new (GetPredicate(RuleTypes.RegexRule)) },
                { RuleTypes.MemoPatternRule, new (GetPredicate(RuleTypes.MemoPatternRule)) },
                { RuleTypes.MemoRegexRule, new(GetPredicate(RuleTypes.MemoRegexRule)) },
                { RuleTypes.DescPatternRule, new (GetPredicate(RuleTypes.DescPatternRule)) },
                { RuleTypes.DescRegexRule, new (GetPredicate(RuleTypes.DescRegexRule)) },
                { RuleTypes.OtherPatternRule, new (GetPredicate(RuleTypes.OtherPatternRule)) },
                { RuleTypes.OtherRegexRule, new (GetPredicate(RuleTypes.OtherRegexRule)) },
            };

    private static BankTransactionRuleResolver<Delegate> GetDelegate(RuleTypes ruleType)
        => AvailableRules.TryGetValue(ruleType, out BankTransactionRuleResolver<Delegate> del)
            ? del : default;

    public static BankTransactionRule<Delegate> GetRule(RuleTypes ruleType, Action<BankTransactionView> action)
        => new(ruleType, GetDelegate(ruleType), action);
}

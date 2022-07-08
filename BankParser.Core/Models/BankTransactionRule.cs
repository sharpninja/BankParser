﻿using System;
using System.Collections.Generic;
using System.Linq;

using ChoETL;

using static BankParser.Core.Models.BankTransactionRuleResolver;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models;

public record struct BankTransactionRule<TDelegate>(
    string RuleName,
    BankTransactionRuleResolver<TDelegate> SelectionResolver,
    Action<BankTransaction> Action
) : IRule
    where TDelegate : Delegate
{
    private bool InvokeResolver(BankTransaction trx)
        => (bool)SelectionResolver.Predicate.DynamicInvoke(trx);

    public void ApplyRule(IEnumerable<BankTransaction> transactions)
    {
        foreach (var trx in transactions
            .Where(InvokeResolver))
        {
            Action(trx);
        }
    }
}

public record struct BankTransactionRule
{
    private static Dictionary<RuleTypes, BankTransactionRuleResolver<Delegate>> AvailableRules
        =>  new ()
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

    public static BankTransactionRule<Delegate> GetRule(RuleTypes ruleType, Action<BankTransaction> action)
        => new (ruleType.ToString(), GetDelegate(ruleType), action);
}

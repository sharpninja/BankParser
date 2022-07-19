using System;
using System.Reflection;
using System.Text.RegularExpressions;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record struct BankTransactionRuleResolver<TDelegate>(TDelegate Predicate)
    where TDelegate : Delegate
{
    public bool IsMatch(BankTransactionView trx)
        => (bool?)Predicate.DynamicInvoke(trx) ?? false;
}

public static class BankTransactionRuleResolver
{
    private static PropertyInfo? memoProperty => typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.Memo));
    private static PropertyInfo? descProperty => typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.Description));
    private static PropertyInfo? otherProperty => typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.OtherParty));

    public static Delegate GetMathHandler(Comparisons comparison)
        => (MathHandler)(comparison switch
        {
            Comparisons.IsGreaterThan => new(IsGreaterThan),
            Comparisons.IsGreaterThanOrEqualTo => new(IsGreaterThanOrEqualTo),
            Comparisons.IsLessThan => new(IsLessThan),
            Comparisons.IsLessThanOrEqualTo => new(IsLessThanOrEqualTo),
            Comparisons.IsEqualTo => new(IsEqualTo),
            _ => throw new NotImplementedException(),
        });

    public static List<Delegate> GetRules(Type type)
        => _predicatesByType.TryGetValue(type, out var handlers)
            ? handlers : new();

    public static List<Delegate> GetRules(PropertyInfo pi)
        => GetRules(pi.PropertyType);

    public static List<Delegate> GetRules(object obj)
        => GetRules(obj.GetType());

    private static Dictionary<Type, List<Delegate>> _predicatesByType = new();

    public static Dictionary<Type, List<Delegate>> PredicateByType
        => _predicatesByType ??= new()
            {
                { typeof(DateOnly), new () { new RuleHandler<MathPredicateParameters>(MathPredicate),} },
                { typeof(decimal), new () { new RuleHandler<MathPredicateParameters>(MathPredicate),} },
                { typeof(string), new () {
                    new RuleHandler<StringParameters>(PatternPredicate),
                    new RuleHandler<RegexParameters>(RegexPredicate),
                } },
            };

    private static Func<IParameters, bool> ByPattern
        => (parms) =>
        {
            if (parms is TransactionPropertyStringParameters p)
            {
                Regex regex = new(p.pattern);
                return regex.IsMatch(p.pi?.GetValue(p.trx)?.ToString() ?? "");
            }

            return default;
        };

    private static Func<IParameters, bool> ByRegex
        => (parms) => parms is TransactionPropertyRegexParameters p
        && p.regex.IsMatch(p.pi?.GetValue(p.trx)?.ToString() ?? "");

    private static bool IsGreaterThan(BankTransactionView trx, PropertyInfo pi, IComparable value)
        => (pi.GetValue(trx) as decimal? as IComparable)?.CompareTo(value) > 0;

    private static bool IsGreaterThanOrEqualTo(BankTransactionView trx, PropertyInfo pi, IComparable value)
            => (pi.GetValue(trx) as decimal? as IComparable)?.CompareTo(value) >= 0;

    private static bool IsLessThan(BankTransactionView trx, PropertyInfo pi, IComparable value)
            => (pi.GetValue(trx) as decimal? as IComparable)?.CompareTo(value) < 0;

    private static bool IsLessThanOrEqualTo(BankTransactionView trx, PropertyInfo pi, IComparable value)
            => (pi.GetValue(trx) as decimal? as IComparable)?.CompareTo(value) <= 0;

    private static bool IsEqualTo(BankTransactionView trx, PropertyInfo pi, IComparable value)
            => (pi.GetValue(trx) as decimal? as IComparable)?.CompareTo(value) == 0;

    public static Delegate GetPredicate(RuleTypes ruleType)
        => ruleType switch
        {
            RuleTypes.PatternRule => PatternPredicate<StringParameters>,
            RuleTypes.RegexRule => RegexPredicate<RegexParameters>,
            RuleTypes.MemoPatternRule => MemoPatternPredicate<StringParameters>,
            RuleTypes.MemoRegexRule => MemoRegexPredicate<StringParameters>,
            RuleTypes.DescPatternRule => DescriptionPatternPredicate<RegexParameters>,
            RuleTypes.DescRegexRule => DescriptionRegexPredicate<RegexParameters>,
            RuleTypes.OtherPatternRule => OtherpartyPatternPredicate<RegexParameters>,
            RuleTypes.OtherRegexRule => OtherPartyRegexPredicate<RegexParameters>,
            RuleTypes.IsEqualToRule => MathPredicate<MathPredicateParameters>,
            RuleTypes.IsLessThanRule => MathPredicate<MathPredicateParameters>,
            RuleTypes.IsLessThanOrEqualToRule => MathPredicate<MathPredicateParameters>,
            RuleTypes.IsGreaterThanRule => MathPredicate<MathPredicateParameters>,
            RuleTypes.IsGreaterThanOrEqualToRule => MathPredicate<MathPredicateParameters>,
            _ => throw new NotImplementedException(),
        };

    public static Delegate PatternPredicate<TParameters>(TParameters parms)
        where TParameters : IParameters
        => (CommonDelegate)(trx => parms is PropertyStringParameters p && ByPattern(p));

    public static Delegate RegexPredicate<TParameters>(TParameters parms)
        where TParameters : IParameters
        => (CommonDelegate)(trx => parms is PropertyRegexParameters p && ByRegex(p));

    public static Delegate MemoRegexPredicate<TParameters>(TParameters parms)
        where TParameters : IParameters
        => (CommonDelegate)(trx => parms is RegexParameters p && ByRegex(trx));

    public static Delegate MemoPatternPredicate<TParameters>(TParameters parms)
        where TParameters : IParameters
        => (CommonDelegate)(trx => parms is StringParameters p && trx is TransactionStringParameters sp
            && ByPattern(new TransactionPropertyStringParameters(sp.trx, memoProperty, p.pattern)));

    public static Delegate DescriptionRegexPredicate<TParameters>(TParameters parms)
        where TParameters : IParameters
        => (CommonDelegate)(trx => parms is RegexParameters p && trx is TransactionRegexParameters sp
            && ByPattern(new TransactionPropertyRegexParameters(sp.trx, memoProperty, p.regex)));

    public static Delegate DescriptionPatternPredicate<TParameters>(TParameters parms)
        where TParameters : IParameters
        => (CommonDelegate)(trx => parms is StringParameters p && trx is TransactionStringParameters sp
            && ByPattern(new TransactionPropertyStringParameters(sp.trx, descProperty, p.pattern)));

    public static Delegate OtherPartyRegexPredicate<TParameters>(TParameters parms)
        where TParameters : IParameters
        => (CommonDelegate)(trx => parms is RegexParameters p && trx is TransactionRegexParameters sp
            && ByPattern(new TransactionPropertyRegexParameters(sp.trx, descProperty, p.regex)));

    public static Delegate OtherpartyPatternPredicate<TParameters>(TParameters parms)
        where TParameters : IParameters
        => (CommonDelegate)(trx => parms is StringParameters p && trx is TransactionStringParameters sp
            && ByPattern(new TransactionPropertyStringParameters(sp.trx, otherProperty, p.pattern)));

    public static Delegate MathPredicate<TParameters>(TParameters parms)
        where TParameters : IParameters
        => (CommonDelegate)(trx => parms is RegexParameters p && trx is TransactionRegexParameters sp
            && ByPattern(new TransactionPropertyRegexParameters(sp.trx, otherProperty, p.regex)));
}

public delegate Delegate RuleHandler<TParameters>(TParameters parms) where TParameters : IParameters;
public delegate Delegate RegexHandler<TParameters>(TParameters parms) where TParameters : IParameters;
public delegate Delegate PatternHandler<TParameters>(TParameters parms) where TParameters : IParameters;
public delegate Delegate PropertyPatternHandler<TParameters>(PropertyInfo pi, TParameters parms) where TParameters : IParameters;
public delegate Delegate PropertyRegexHandler(PropertyInfo pi, Regex regex);
public delegate Delegate MathRuleHandler(PropertyInfo pi, IComparable value, CalculationHandler calculation);
public delegate bool CalculationHandler(BankTransactionView trx, PropertyInfo pi, IComparable value);
public delegate bool ByPatternHandler(BankTransactionView trx, PropertyInfo pi, IParameters parms);
public delegate bool ByRegexHandler(BankTransactionView trx, PropertyInfo pi, Regex regex);
public delegate bool MathHandler(BankTransactionView trx, PropertyInfo pi, IComparable value);
public delegate bool CommonDelegate(IParameters parms);

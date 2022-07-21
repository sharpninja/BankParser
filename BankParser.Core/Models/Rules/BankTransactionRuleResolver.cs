using System;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

using GPS.IServiceProvider.Extensions;

public record struct BankTransactionRuleResolver<TDelegate>(TDelegate Predicate)
    where TDelegate : Delegate
{
    public bool IsMatch(BankTransactionView trx)
        => (bool?)Predicate.DynamicInvoke(trx) ?? false;
}

public static class BankTransactionRuleResolver
{
    private static PropertyInfo? MemoProperty
        => typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.Memo));

    private static PropertyInfo? DescProperty
        => typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.Description));

    private static PropertyInfo? OtherProperty
        => typeof(BankTransactionView).GetProperty(nameof(BankTransactionView.OtherParty));

    public static CalculationHandler GetMathHandler(Comparisons comparison)
        => comparison switch
        {
            Comparisons.IsGreaterThan => IsGreaterThan,
            Comparisons.IsGreaterThanOrEqualTo => IsGreaterThanOrEqualTo,
            Comparisons.IsLessThan => IsLessThan,
            Comparisons.IsLessThanOrEqualTo => IsLessThanOrEqualTo,
            Comparisons.IsEqualTo => IsEqualTo,
            _ => throw new NotImplementedException(),
        };

    public static ByRegexHandler GetRegexHandler(RegexParameters parameters)
        => RegexHandler;

    public static ByPatternHandler GetPatternHandler(StringParameters parameters)
        => PatternHandler;

    public static List<Func<BankTransactionView, IParameters, CommonDelegate>> GetRules(Type type)
        => _predicatesByType.TryGetValue(type, out List<Func<BankTransactionView, IParameters,
            CommonDelegate>>? handlers)
            ? handlers
            : new();

    public static List<Func<BankTransactionView, IParameters, CommonDelegate>> GetRules(PropertyInfo pi)
        => GetRules(pi.PropertyType);

    public static List<Func<BankTransactionView, IParameters, CommonDelegate>> GetRules(object obj)
        => GetRules(obj.GetType());

    private static readonly Dictionary<Type, List<Func<BankTransactionView, IParameters,
        CommonDelegate>>> _predicatesByType = new();

    public static Dictionary<Type, List<Func<BankTransactionView, IParameters, CommonDelegate>>> PredicateByType => _predicatesByType;

    private static Func<IParameters, bool> ByPattern => static parameters =>
    {
        if (parameters is not TransactionPropertyStringParameters p)
        {
            return default;
        }

        Regex regex = new(p.Pattern);

        return regex.IsMatch(
            p.Pi?.GetValue(p.Trx)
                ?.ToString() ??
            ""
        );

    };

    private static Func<IParameters, bool> ByRegex => static parameters
        => parameters is TransactionPropertyRegexParameters p &&
           p.Regex.IsMatch(
               p.Pi?.GetValue(p.Trx)
                   ?.ToString() ??
               ""
           );

    private static bool IsGreaterThan(BankTransactionView trx, PropertyInfo pi, IComparable value)
        => (pi.GetValue(trx) as decimal?)?.CompareTo(value) > 0;

    private static bool IsGreaterThanOrEqualTo(
        BankTransactionView trx,
        PropertyInfo pi,
        IComparable value
    )
        => (pi.GetValue(trx) as decimal?)?.CompareTo(value) >= 0;

    private static bool IsLessThan(BankTransactionView trx, PropertyInfo pi, IComparable value)
        => (pi.GetValue(trx) as decimal?)?.CompareTo(value) < 0;

    private static bool IsLessThanOrEqualTo(
        BankTransactionView trx,
        PropertyInfo pi,
        IComparable value
    )
        => (pi.GetValue(trx) as decimal?)?.CompareTo(value) <= 0;

    private static bool RegexHandler(
        BankTransactionView trx,
        PropertyInfo pi,
        RegexParameters value
    )
    {
        return Invoke(pi.Name);

        bool Invoke(string name)
        {
            CommonDelegate predicate = name switch
            {
                nameof(BankTransactionView.OtherParty) => OtherPartyRegexPredicate(trx, value),
                nameof(BankTransactionView.Memo) => MemoRegexPredicate(trx, value),
                nameof(BankTransactionView.Description) => DescriptionRegexPredicate(trx, value),
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
            };

            return predicate.Invoke(trx, new PropertyRegexParameters(pi, value));
        }
    }

    private static bool PatternHandler(
        BankTransactionView trx,
        PropertyInfo pi,
        StringParameters value)
    {
        return Invoke(pi.Name);

        bool Invoke(string name)
        {
            CommonDelegate predicate = name switch
            {
                nameof(BankTransactionView.OtherParty) => OtherPartyPatternPredicate(trx, value),
                nameof(BankTransactionView.Memo) => MemoPatternPredicate(trx, value),
                nameof(BankTransactionView.Description) => DescriptionPatternPredicate(trx, value),
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
            };

            return predicate.Invoke(trx, new PropertyStringParameters(pi, value));
        }
    }

    private static bool IsEqualTo(BankTransactionView trx, PropertyInfo pi, IComparable value)
        => (pi.GetValue(trx) as decimal?)?.CompareTo(value) == 0;

    public static Delegate GetPredicate(RuleTypes ruleType)
        => ruleType switch
        {
            RuleTypes.PatternRule => PatternPredicate<StringParameters>,
            RuleTypes.RegexRule => RegexPredicate<RegexParameters>,
            RuleTypes.MemoPatternRule => MemoPatternPredicate<StringParameters>,
            RuleTypes.MemoRegexRule => MemoRegexPredicate<StringParameters>,
            RuleTypes.DescPatternRule => DescriptionPatternPredicate<RegexParameters>,
            RuleTypes.DescRegexRule => DescriptionRegexPredicate<RegexParameters>,
            RuleTypes.OtherPatternRule => BankTransactionRuleResolver.OtherPartyPatternPredicate<RegexParameters>,
            RuleTypes.OtherRegexRule => OtherPartyRegexPredicate<RegexParameters>,
            RuleTypes.IsEqualToRule => MathPredicate<MathPredicateParameters>,
            RuleTypes.IsLessThanRule => MathPredicate<MathPredicateParameters>,
            RuleTypes.IsLessThanOrEqualToRule => MathPredicate<MathPredicateParameters>,
            RuleTypes.IsGreaterThanRule => MathPredicate<MathPredicateParameters>,
            RuleTypes.IsGreaterThanOrEqualToRule => MathPredicate<MathPredicateParameters>,
            _ => throw new NotImplementedException(),
        };

    static BankTransactionRuleResolver()
    {
        _predicatesByType.Add(
            typeof(string),
            new List<Func<BankTransactionView, IParameters, CommonDelegate>>
            {
                PatternPredicate,
                MemoPatternPredicate,
                DescriptionPatternPredicate,
            }
        );
        _predicatesByType.Add(
            typeof(Regex),
            new List<Func<BankTransactionView, IParameters, CommonDelegate>>
            {
                RegexPredicate,
                MemoRegexPredicate,
                DescriptionRegexPredicate,
            }
        );
        _predicatesByType.Add(
            typeof(IComparable),
            new List<Func<BankTransactionView, IParameters, CommonDelegate>>
            {
                RegexPredicate,
                MemoRegexPredicate,
                DescriptionRegexPredicate,
            }
        );
    }

    public static CommonDelegate PatternPredicate<TParameters>(BankTransactionView trx, TParameters parameters)
        where TParameters : IParameters
        => new(
            (_,_)
                => parameters is PropertyStringParameters p && BankTransactionRuleResolver.ByPattern(p),
            typeof(PatternHandler));

    public static CommonDelegate RegexPredicate<TParameters>(BankTransactionView trx, TParameters parameters)
        where TParameters : IParameters
        => new(
            (_,_)
                => parameters is PropertyRegexParameters p && ByRegex(p),
            typeof(RegexHandler));

    public static CommonDelegate MemoRegexPredicate<TParameters>(BankTransactionView trx, TParameters parameters)
        where TParameters : IParameters
    {
        bool Func(BankTransactionView _, IParameters parms)
            => parameters is RegexParameters p &&
               ByPattern(new TransactionPropertyRegexParameters(trx, MemoProperty, p.Regex));

        return new(Func, typeof(PropertyRegexHandler));
    }

    public static CommonDelegate MemoPatternPredicate<TParameters>(BankTransactionView trx, TParameters parameters)
        where TParameters : IParameters
    {
        bool Func(BankTransactionView _, IParameters parms)
            => parameters is StringParameters p &&
               ByPattern(new TransactionPropertyStringParameters(trx, MemoProperty, p.Pattern));

        return new(Func, typeof(PropertyPatternHandler));
    }

    public static CommonDelegate DescriptionRegexPredicate<TParameters>(BankTransactionView trx, TParameters parameters)
        where TParameters : IParameters
    {
        bool Func(BankTransactionView _, IParameters parms)
            => parameters is RegexParameters p &&
               ByPattern(new TransactionPropertyRegexParameters(trx, DescProperty, p.Regex));

        return new(Func, typeof(PropertyRegexHandler));
    }

    public static CommonDelegate DescriptionPatternPredicate<TParameters>(BankTransactionView trx, TParameters parameters)
        where TParameters : IParameters
    {
        bool Func(BankTransactionView _, IParameters parms)
            => parameters is StringParameters p &&
               ByPattern(new TransactionPropertyStringParameters(trx, DescProperty, p.Pattern));

        return new(Func, typeof(PropertyPatternHandler));
    }

    public static CommonDelegate OtherPartyRegexPredicate<TParameters>(BankTransactionView trx, TParameters parameters)
        where TParameters : IParameters
    {
        bool Func(BankTransactionView _, IParameters parms)
            => parameters is RegexParameters p &&
               ByPattern(new TransactionPropertyRegexParameters(
                   trx,
                   OtherProperty,
                   p.Regex));

        return new(
            Func,
            typeof(PropertyRegexHandler));
    }

    public static CommonDelegate OtherPartyPatternPredicate<TParameters>(BankTransactionView trx, TParameters parameters)
        where TParameters : IParameters
    {
        bool Func(BankTransactionView _, IParameters parms)
            => parameters is StringParameters p &&
               ByPattern(new TransactionPropertyStringParameters(trx, OtherProperty, p.Pattern));

        return new(Func, typeof(PropertyPatternHandler));
    }

    public static CommonDelegate MathPredicate<TParameters>(BankTransactionView trx, TParameters parameters)
        where TParameters : IParameters
        => new(
            (_, _) => parameters is MathPredicateParameters p &&
                      BankTransactionRuleResolver.ByPattern(new MathPredicateParameters(p.Pi, p.Value, p.Calculation)),
            typeof(MathRuleHandler)
        );

    public static IServiceCollection AddRulePredicates(this IServiceCollection services)
    {
        string[] ruleTypeNames = Enum.GetNames<RuleTypes>();

        foreach (string name in ruleTypeNames)
        {
            RuleTypes ruleType = Enum.Parse<RuleTypes>(name);
            Delegate predicate = BankTransactionRuleResolver.GetPredicate(ruleType);
            services.AddTransient(_ => predicate);
        }

        return services;
    }

    public static IServiceCollection AddRuleParameters(this IServiceCollection services)
    {
        services.AddTransient<MathPredicateParameters>();
        services.AddTransient<RegexParameters>();
        services.AddTransient<StringParameters>();
        services.AddTransient<TransactionRegexParameters>();
        services.AddTransient<TransactionStringParameters>();
        services.AddTransient<PropertyRegexParameters>();
        services.AddTransient<PropertyStringParameters>();

        return services;
    }

    public static IServiceCollection AddDefaultDelegateHandlers(this IServiceCollection services)
    {
        services.AddTransient(static _ => DelegateFactory.DefaultCalculationHandler);
        services.AddTransient(static _ => DelegateFactory.DefaultRegexHandler);
        services.AddTransient(static _ => DelegateFactory.DefaultPatternHandler);

        return services;
    }
}

public class DelegateFactory
{
    public Delegate? GetHandler<THandler>(DependencyInitializer initializer)
        => default(THandler) switch
        {
            CalculationHandler => InitializeCalculationHandler(initializer),
            ByRegexHandler => InitializeRegexHandler(initializer),
            ByPatternHandler => InitializePatternHandler(initializer),
            PropertyHandler => InitializePatternHandler(initializer),
            _ => default,
        };

    public static CalculationHandler DefaultCalculationHandler { get; set; }
        = static (_, _, _)
            => default;

    public static ByRegexHandler DefaultRegexHandler { get; set; }
        = static (_, _, _)
        => default;

    public static ByPatternHandler DefaultPatternHandler { get; set; }
        = static (_, _, _)
        => default;

    private static Delegate InitializePropertyHandler(DependencyInitializer initializer)
    {
        ValueWrapper<RegexParameters> value = new();
        initializer.Apply(value);

        return BankTransactionRuleResolver.GetRegexHandler(value.Value!);
    }

    private static Delegate InitializePatternHandler(DependencyInitializer initializer)
    {
        ValueWrapper<StringParameters> value = new();
        initializer.Apply(value);

        return BankTransactionRuleResolver.GetPatternHandler(value.Value!);
    }

    private static Delegate InitializeRegexHandler(DependencyInitializer initializer)
    {
        ValueWrapper<RegexParameters> value = new();
        initializer.Apply(value);

        return BankTransactionRuleResolver.GetRegexHandler(value.Value!);
    }

    private static Delegate InitializeCalculationHandler(DependencyInitializer initializer)
    {
        ValueWrapper<Comparisons> value = new();
        initializer.Apply(value);

        return BankTransactionRuleResolver.GetMathHandler(value.Value);
    }
}

public class ValueWrapper<TValue>

{
    public TValue? Value
    {
        get;
        set;
    }
}

public delegate CommonDelegate RuleHandler<in TParameters>(TParameters parameters) where TParameters : IParameters;
public delegate CommonDelegate RegexHandler(RegexParameters parameters);
public delegate CommonDelegate PatternHandler(StringParameters parameters);
public delegate CommonDelegate PropertyPatternHandler(PropertyInfo pi, string pattern);
public delegate CommonDelegate PropertyRegexHandler(PropertyInfo pi, Regex regex);
public delegate CommonDelegate MathRuleHandler(PropertyInfo pi, IComparable value, CalculationHandler calculation);
public delegate bool CalculationHandler(BankTransactionView trx, PropertyInfo pi, IComparable value);
public delegate bool ByPatternHandler(BankTransactionView trx, PropertyInfo pi, StringParameters parameters);
public delegate bool ByRegexHandler(BankTransactionView trx, PropertyInfo pi, RegexParameters regex);
public delegate bool PropertyHandler(BankTransactionView trx, PropertyInfo pi, IParameters regex);
namespace BankParser.ViewModels;

using Core.Models.Rules;

public class RuleAction
{
    public RuleActionDelegate RuleDelegate
    {
        get;
    }

    public string RuleActionName
    {
        get;
    }


    public RuleAction(RuleActionDelegate ruleDelegate, string ruleActionName)
    {
        RuleDelegate = ruleDelegate;
        RuleActionName = ruleActionName;
    }

    public void ApplyRuleAction(
        RuleViewModel ruleViewModel,
        BankTransactionView trx,
        params object[] parameters
    )
        => RuleDelegate.DynamicInvoke(trx, parameters);

    private static bool DoesRuleApply(
        RuleViewModel ruleViewModel,
        BankTransactionView trx,
        params object[] parameters
    )
    {
        bool result = false;

        foreach (IRule rule in ruleViewModel.Rules)
        {
            RuleTypes ruleType = rule.RuleType;
            Delegate predicate =BankTransactionRuleResolver.GetPredicate(ruleType);

            IParameters? param = ruleType switch
            {
                RuleTypes.PatternRule => App.GetService<StringParameters>(),
                RuleTypes.RegexRule => App.GetService<RegexParameters>(),
                RuleTypes.MemoPatternRule => App.GetService<StringParameters>(),
                RuleTypes.MemoRegexRule => App.GetService<StringParameters>(),
                RuleTypes.DescPatternRule => App.GetService<RegexParameters>(),
                RuleTypes.DescRegexRule => App.GetService<RegexParameters>(),
                RuleTypes.OtherPatternRule => App.GetService<RegexParameters>(),
                RuleTypes.OtherRegexRule => App.GetService<RegexParameters>(),
                RuleTypes.IsEqualToRule => App.GetService<MathPredicateParameters>(),
                RuleTypes.IsLessThanRule => App.GetService<MathPredicateParameters>(),
                RuleTypes.IsLessThanOrEqualToRule => App.GetService<MathPredicateParameters>(),
                RuleTypes.IsGreaterThanRule => App.GetService<MathPredicateParameters>(),
                RuleTypes.IsGreaterThanOrEqualToRule => App.GetService<MathPredicateParameters>(),
                _ => default,
            };

            if (param is null)
            {
                continue;
            }

            param.MapParameters(param.GetType(), parameters);
            result |= (bool)predicate.DynamicInvoke(param)!;

            if (result)
            {
                return true;
            }
        }

        return result;
    }

    public static void IsVisibleOnMatchRuleAction(
        BankTransactionView trx,
        params object[] parameters
    )
    {
        Regex? regex = GetRegexFromParameters(parameters);

        if (regex is null)
        {
            return;
        }

        bool isMatch = false;

        foreach (var pi in BankTransactionView.Columns)
        {
            if (pi.PropertyType != typeof(string))
            {
                continue;
            }

            isMatch |= regex.IsMatch((string)pi.GetValue(trx)!);

            if (isMatch)
            {
                break;
            }
        }

        trx.Visibility = isMatch
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static Regex? BuildRegex(string? pattern)
        => pattern is not null
            ? new(Regex.Escape(pattern), RegexOptions.IgnoreCase)
            : null;

    private static string? GetPatternFromParameters(object[] parameters)
    {
        if ((parameters.Length > 0) &&
            parameters[0] is string pattern)
        {
            return pattern;
        }

        return null;
    }

    private static Regex? GetRegexFromParameters(object[] parameters)
    {
        if ((parameters.Length > 0) &&
            parameters[0] is Regex regex)
        {
            return regex;
        }

        return BuildRegex(GetPatternFromParameters(parameters));
    }
}

public delegate void RuleActionDelegate(BankTransactionView trx, params object[] parameters);
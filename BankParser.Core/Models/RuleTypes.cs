// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models;

public enum RuleTypes
{
    PatternRule, RegexRule,
    MemoPatternRule, MemoRegexRule,
    DescPatternRule, DescRegexRule,
    OtherPatternRule, OtherRegexRule,
    IsEqualToRule,
    IsLessThanRule, IsLessThanOrEqualToRule,
    IsGreaterThanRule, IsGreaterThanOrEqualToRule,
}

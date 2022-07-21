

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record PropertyRegexParameters(PropertyInfo Pi, Regex Regex) : IParameters
{
    public PropertyRegexParameters(PropertyInfo pi, RegexParameters parameters):
        this(pi, parameters.Regex){}
}

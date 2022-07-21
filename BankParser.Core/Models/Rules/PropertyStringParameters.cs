

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record PropertyStringParameters(PropertyInfo Pi, string Pattern) : IParameters
{
    public PropertyStringParameters(PropertyInfo pi, StringParameters parameters)
        : this(pi, parameters.Pattern)
    {
    }

}

using System.Reflection;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record PropertyStringParameters(PropertyInfo pi, string pattern) : IParameters
{
}

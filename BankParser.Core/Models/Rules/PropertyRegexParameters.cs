using System.Reflection;
using System.Text.RegularExpressions;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record PropertyRegexParameters(PropertyInfo pi, Regex regex) : IParameters
{
}

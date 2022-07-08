using System.Reflection;
using System.Text.RegularExpressions;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models;

public record PropertyRegexParameters(PropertyInfo pi, Regex regex) : IParameters
{
}

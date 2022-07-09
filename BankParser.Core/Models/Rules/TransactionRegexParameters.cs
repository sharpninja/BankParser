using System.Reflection;
using System.Text.RegularExpressions;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record TransactionPropertyRegexParameters(
    BankTransaction trx,
    PropertyInfo? pi,
    Regex regex) : IParameters
{
}
public record TransactionRegexParameters(
    BankTransaction trx,
    Regex regex) : IParameters
{
}

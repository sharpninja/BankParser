

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record TransactionPropertyRegexParameters(
    BankTransactionView Trx,
    PropertyInfo? Pi,
    Regex Regex) : IParameters
{
}
public record TransactionRegexParameters(
    BankTransactionView Trx,
    Regex Regex) : IParameters
{
}

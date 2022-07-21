

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record TransactionPropertyStringParameters(
    BankTransactionView Trx,
    PropertyInfo? Pi,
    string Pattern) : IParameters
{
}

public record TransactionStringParameters(
    BankTransactionView Trx,
    string Pattern) : IParameters
{
}

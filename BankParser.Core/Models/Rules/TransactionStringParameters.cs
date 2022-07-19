using System.Reflection;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record TransactionPropertyStringParameters(
    BankTransactionView trx,
    PropertyInfo? pi,
    string pattern) : IParameters
{
}

public record TransactionStringParameters(
    BankTransactionView trx,
    string pattern) : IParameters
{
}

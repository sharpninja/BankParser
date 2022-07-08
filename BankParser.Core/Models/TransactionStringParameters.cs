using System.Reflection;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models;

public record TransactionPropertyStringParameters(BankTransaction trx, PropertyInfo pi, string pattern) : IParameters
{
}
public record TransactionStringParameters(BankTransaction trx, string pattern) : IParameters
{
}

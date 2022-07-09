using System;
using System.Reflection;
using static BankParser.Core.Models.Rules.BankTransactionRuleResolver;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record MathPredicateParameters(PropertyInfo pi, IComparable value, CalculationHandler calculation) : IParameters
{
}

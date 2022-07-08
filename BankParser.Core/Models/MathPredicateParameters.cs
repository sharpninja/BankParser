using System;
using System.Reflection;

using static BankParser.Core.Models.BankTransactionRuleResolver;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models;

public record MathPredicateParameters(PropertyInfo pi, IComparable value, CalculationHandler calculation) : IParameters
{
}

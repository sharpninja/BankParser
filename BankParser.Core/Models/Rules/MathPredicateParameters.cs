using System;
using System.Reflection;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record MathPredicateParameters(PropertyInfo pi, IComparable value, CalculationHandler calculation) : IParameters
{
}

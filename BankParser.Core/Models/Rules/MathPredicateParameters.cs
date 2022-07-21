using System;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Rules;

public record MathPredicateParameters(
    PropertyInfo Pi,
    IComparable Value,
    CalculationHandler Calculation
) : IParameters
{
}

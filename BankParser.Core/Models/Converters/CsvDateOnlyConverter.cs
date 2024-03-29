﻿using System;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Converters;

using System.Globalization;

using ChoETL;

public class CsvDateOnlyConverter : IChoConvertible
{
    public bool Convert(
        string propName,
        object propValue,
        CultureInfo culture,
        out object? convPropValue)
    {
        convPropValue = propValue switch
        {
            string s => DateOnly.Parse(s),
            DateOnly d => d.ToString(),
            _ => null,
        };

        return convPropValue is not null;
    }
    public bool ConvertBack(
        string propName,
        object propValue,
        Type targetType,
        CultureInfo culture,
        out object? convPropValue)
    {
        convPropValue = propValue switch
        {
            string s => DateOnly.Parse(s, culture),
            DateOnly d => d.ToString(),
            _ => null,
        };

        return convPropValue is not null;
    }
}
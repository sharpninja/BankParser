using System;

namespace BankParser.Core.Models;

public record struct DateGroup(DateOnly Date, List<object> Children, string Type = "", string OtherParty = "", decimal? AmountDebit = null, decimal? AmountCredit = null )
{
    public static implicit operator (DateOnly Date, List<object> Children)(DateGroup value)
    {
        return (value.Date, value.Children);
    }

    public static implicit operator DateGroup((DateOnly Date, List<object> Children) value)
    {
        return new DateGroup(value.Date, value.Children);
    }
}
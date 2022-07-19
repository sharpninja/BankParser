using System;
using System.Collections.Concurrent;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models;

public record struct BankTransactionMetadata(
    BankTransactionView Transaction,
    ConcurrentDictionary<string, object> Properties,
    ConcurrentDictionary<string, Delegate> Rules
    )
{
}

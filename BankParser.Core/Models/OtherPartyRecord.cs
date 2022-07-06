using System;
// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models;

public record OtherPartyRecord(string Name, string Address, string Phone, DateTimeOffset? Date, string Other);


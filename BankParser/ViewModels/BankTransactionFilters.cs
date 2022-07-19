using System.Reflection;

using BankParser.Core.Models;

namespace BankParser.ViewModels;

internal record struct BankTransactionFilters(
    BankTransactionView Trx,
    Dictionary<PropertyInfo, Predicate<BankTransactionView>> Map
);

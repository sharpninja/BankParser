namespace BankParser.ViewModels;

internal record struct BankTransactionFilters(
    BankTransactionView Trx,
    Dictionary<PropertyInfo, Predicate<BankTransactionView>> Map
);

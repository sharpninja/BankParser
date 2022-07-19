namespace BankParser.Core.Models;

public record ImmutableBankTransaction(
    string? TransactionNumber = null,
    DateOnly? Date = null,
    string? Memo = null,
    string? Description = null,
    decimal? AmountCredit = null,
    decimal? AmountDebit = null,
    decimal? Fees = null);

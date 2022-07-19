using BankParser.Core.Models;

namespace BankParser.Core.Contracts.Services;

public interface ITransactionService
{
    List<BankTransactionView> Unmodified
    {
        get;
    }

    List<ImmutableBankTransaction> LoadData(string fileName);

    IEnumerable<ImmutableBankTransaction> FromCsv(string fileName);

    IEnumerable<ImmutableBankTransaction> FromJson(string json);

    List<BankTransactionView> FromImmutable(IEnumerable<ImmutableBankTransaction> unfiltered)
        => unfiltered.Select(static trx => new BankTransactionView(trx)).ToList();
}

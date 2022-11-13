namespace BankParser.Core.Services;

using ChoETL;

using Models.Converters;

public class TransactionService : ITransactionService
{
    private List<ImmutableBankTransaction>? _immutable;
    public const string DEFAULT_FILENAME = @"C:\Users\kingd\OneDrive\Desktop\transactions.json";

    public IEnumerable<ImmutableBankTransaction>? FromJson(string json)
        => JsonConvert.DeserializeObject<ImmutableBankTransaction[]>(json, Converter._settings);

    [ JsonIgnore ]
    private List<ImmutableBankTransaction> Immutable
    {
        get => _immutable ?? GetDefaultFile();
        set
        {
            if (value == _immutable)
            {
                return;
            }

            _immutable = value;
            Unmodified = BankTransactionView.FromImmutable(_immutable);
        }
    }

    public List<ImmutableBankTransaction> LoadData(string fileName)
    {
        List<ImmutableBankTransaction> data = new();
        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            data = FromJson(File.ReadAllText(fileName))?.ToList() ??
                        new();
        }
        else if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            data = FromCsv(fileName)?.ToList() ?? new();
        }

        Immutable = data;

        return data;
    }

    internal List<ImmutableBankTransaction> GetDefaultFile()
        => LoadData(DEFAULT_FILENAME);

    public List<BankTransactionView> Unmodified
    {
        get;
        set;
    } = null!;

    public IEnumerable<ImmutableBankTransaction>? FromCsv(string fileName)
    {
        static decimal? ReturnNullDecimal() => null;

        static decimal? ParseDecimal(string item)
            => decimal.TryParse(item, out decimal data)
                ? data
                : ReturnNullDecimal();

        //var stream = File.OpenText(fileName);
        List<string> lines = File.ReadAllLines(fileName).ToList();

        int headerRow = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            string trimmed = line.Trim('\"');

            if (!trimmed.StartsWith("Transaction", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            headerRow = i - 1;
            break;
        }

        for (int i = headerRow; i >= 0; --i)
        {
            lines.RemoveAt(i);
        }

        ChoCSVLiteReader reader = new();
        IEnumerable<string[]> rows = reader.ReadLines(lines);

        List<string> fieldNames = new();
        foreach (string[] values in rows)
        {
            Dictionary<string, string?> data = new();
            string? currentField = fieldNames.FirstOrDefault();
            bool inQuoted = false;
            List<string> quotedSections = new();
            bool isHeaderRow = !fieldNames.Any();
            Queue<string> fields = new(fieldNames);

            for (int i = 0; i < values.Length; i++)
            {
                if (isHeaderRow)
                {
                    fieldNames.Add(values[i]);
                }
                else
                {
                    string value = values[i]?.Trim() ?? "";
                    if (currentField is not null)
                    {
                        currentField = fields.Dequeue();
                        data.Add(currentField, value);
                        continue;
                    }
                    if (currentField is not null && inQuoted &&
                        value.EndsWith("\"", StringComparison.InvariantCultureIgnoreCase))
                    {
                        quotedSections.Add(value.TrimEnd('\"'));
                        inQuoted = false;
                        value = string.Join(", ", quotedSections);
                        data.Add(currentField, value);
                    }
                    else if (value.StartsWith("\"", StringComparison.Ordinal))
                    {
                        quotedSections.Clear();
                        quotedSections.Add(value.TrimStart('\"'));
                        currentField = fields.Dequeue();
                        inQuoted = true;
                    }
                    else if (inQuoted)
                    {
                        quotedSections.Add(value);
                    }
                    else
                    {
                        currentField = fields.Dequeue();
                        data.Add(currentField, value);
                    }
                }
            }

            if (data.Count <= 0)
            {
                continue;
            }

            yield return new(
                data.TryGetValue("Transaction Number", out string? tr) ? tr : null,
                data.TryGetValue("Date", out string? date) ? DateOnly.Parse(date!) : null,
                data.TryGetValue(nameof(BankTransactionView.Memo), out string? memo) ? memo : null,
                data.TryGetValue("Description", out string? desc) ? desc : null,
                data.TryGetValue("Amount Credit", out string? credit) ? ParseDecimal(credit!) : null,
                data.TryGetValue("Amount Debit", out string? debit) ? ParseDecimal(debit!) : null
            );

            data.Clear();
        }

    }
}

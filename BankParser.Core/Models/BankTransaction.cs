using System;
using System.Reflection;
using System.Text.RegularExpressions;
using BankParser.Core.Models.Converters;
using ChoETL;

using Newtonsoft.Json;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models;

public partial class BankTransaction
{
    private string? _type;
    private string? _otherParty;

    [JsonProperty("Date"), JsonConverter(typeof(DateOnlyConverter)), ]
    public DateOnly? Date
    {
        get;
        set;
    }

    [JsonIgnore]
    public string? Type => _type ??= ParseDescription();

    [JsonIgnore]
    public string OtherParty => _otherParty ??= ParseMemo().Name;

    [JsonProperty("Amount Debit")]
    public decimal? AmountDebit
    {
        get;
        set;
    }

    [JsonIgnore]
    public string? AmountDebitString => AmountDebit?.ToString("C2");

    [JsonProperty("Amount Credit")]
    [ChoCSVRecordField(FieldName = "Amount Credit")]
    public decimal? AmountCredit
    {
        get;
        set;
    }

    [JsonIgnore]
    public string? AmountCreditString => AmountCredit?.ToString("C2");

    [JsonProperty("Fees")]
    public decimal? Fees
    {
        get;
        set;
    }

    [JsonProperty("Index")]
    [JsonConverter(typeof(ParseStringConverter))]
    public long Index
    {
        get;
        set;
    }

    [JsonProperty("Transaction Number")]
    public string? TransactionNumber
    {
        get;
        set;
    } = null!;

    [JsonProperty("Description")]
    public string? Description
    {
        get;
        set;
    } = null!;

    [JsonProperty(nameof(BankTransaction.Memo))]
    public string? Memo
    {
        get;
        set;
    } = null!;

    [JsonProperty(nameof(BankTransaction.Metadata))]
    public BankTransactionMetadata Metadata
    {
        get;
        set;
    } = new();

    [JsonIgnore]
    public IEnumerable<string> PotentialFilters
    {
        get
        {
            var results = OtherParty
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(s
                => s.Split('*', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ).ToList();

            var newResult = results.ToList();

            for (int i = 0; i < results.Count; i++)
            {
                string toFind = i is 0
                    ? results[i]
                    : ' ' + results[i];

                if (i is 0 && toFind is "PP" && OtherParty.StartsWith("PP*"))
                {
                    toFind = "PP*";
                    newResult.Insert(0, toFind);
                    continue;
                }

                if (!OtherParty.Contains(toFind))
                {
                    toFind = $"*{toFind.Trim()}";
                    if (!OtherParty.Contains(toFind))
                    {
                        continue;
                    }
                }
                Index index = OtherParty.IndexOf(toFind);
                int nextSpace = OtherParty.IndexOf(' ', index.Value);
                if (nextSpace > -1)
                {
                    int secondSpace = OtherParty.IndexOf(' ', nextSpace + 1);
                    if (secondSpace > -1)
                    {
                        string term = OtherParty[index..(Index)secondSpace];
                        if (term.Equals("pp", StringComparison.OrdinalIgnoreCase))
                        {
                            term = "PP*";
                        }
                        newResult.Insert(0, term);
                    }
                }
                newResult.Insert(0, OtherParty[index..]);
            }

            newResult.Insert(0, OtherParty);

            return newResult;
        }
    }

    private string? ParseDescription()
    {
        if(Description is null)
        {
            return null;
        }

        string[] words = Description.ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.Trim())
            .Distinct()
            .ToArray();

        return (words.First(), words.Last()) switch
        {
            ("withdrawal", "debit") => "Debit Out",
            ("deposit", "transfer") => "Transfer In",
            ("withdrawal", "banking") => "Transfer Out",
            ("withdrawal", "share") => "Transfer Out",
            ("withdrawal", _) => Description.Contains("Transfer", StringComparison.InvariantCultureIgnoreCase)
                ? "Transfer"
                : "Debit Out",
            ("deposit", "check") => "Deposit",
            ("deposit", "dc") => "Deposit",
            ("deposit", "deposit") => "Deposit",
            ("deposit", _) => "Deposit",
            ("ach", "fee") => "Fee: NSF",
            ("foreign", "fee") => "Fee: ATM",
            ("courtesy", "fee") => "Fee: Courtesy",
            ("transfer", "fee") => "Fee: Transfer",
            (_, "fee") => $"Fee: {Description}",
            ("transaction", "comment") => "Comment",
            _ => $"UNKOWN: [{Description}]",
        };
    }

    private string[] ParseOtherParty(string otherParty)
    {
        string[] parts = otherParty.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<string> result = new();

        int i = Array.IndexOf(parts, "%%");
        Index index = default;
        Index start = new(0);
        if (i > -1)
        {
            index = new Index(i);
        }

        string first = parts.First();
        string last = parts.Last();

        if (i == 0)
        {
            start = new(1);
            result.Add(string.Join(' ', parts[start..]));
        }
        else if (last == first)
        {
            result.Add(first);
        }
        else if (first.StartsWith('#'))
        {
            start = new(1);
            if (i > -1)
            {
                result.Add(string.Join(' ', parts[start..index]));
            }
            else
            {
                result.Add(string.Join(' ', parts[start..]));
            }
        }
        else if (_debitParser.IsMatch(otherParty))
        {
            (bool match, OtherPartyRecord otherParty) matchResult = Match(_debitParser, 4,
                groupArray => new(groupArray[1].Value, null, null,
                    DateTimeOffset.Parse(groupArray[2].Value), groupArray[3].Value));
            if (matchResult.match && matchResult.otherParty != default)
            {
                result.Add(matchResult.otherParty.Name);
                result.Add(matchResult.otherParty.Address ?? "");
                result.Add(matchResult.otherParty.Phone ?? "");
                result.Add(matchResult.otherParty.Date?.ToString() ?? "");
            }
        }

        if ("CA TN WA KS".IndexOf(last, StringComparison.Ordinal) <= -1)
        {
            return result.ToArray();
        }

        if ((result.Any() && result[0].EndsWith(last)) ||
            !result.Any())
        {
            result.Clear();
            result.Add(string.Join(' ', parts[start..^2]));
        }

        if (result.Count > 1)
        {
            result.Insert(1, $"{parts[^2]} {last}");
        }
        else
        {
            result.Add($"{parts[^2]} {last}");
        }

        return result.ToArray();
    }

    private (bool match, OtherPartyRecord otherParty) Match(
        Regex regex, int expectedCount, Func<Group[], OtherPartyRecord?> factory)
    {
        string? toParse = Memo ?? Description;

        if(toParse is null)
        {
            return default;
        }

        Match? matches = regex.Matches(toParse).FirstOrDefault();
        GroupCollection? groups = matches?.Groups;
        if (groups?.Count != expectedCount)
        {
            return (false, default);
        }

        Group[]? groupArray = groups?.Values.ToArray();
        if (groupArray is not null)
        {
            OtherPartyRecord otherParty = factory.Invoke(groupArray) ?? default;

            if (otherParty != default)
            {
                string[] parsedOtherParty = ParseOtherParty(otherParty.Name);

                if (parsedOtherParty.Length > 0)
                {
                    otherParty = otherParty with { Name = parsedOtherParty[0],};
                }

                if (parsedOtherParty.Length > 1)
                {
                    otherParty = otherParty with { Address = parsedOtherParty[1],};
                }

                if (parsedOtherParty.Length > 2)
                {
                    otherParty = otherParty with { Phone = parsedOtherParty[2],};
                }

                if (parsedOtherParty.Length > 3)
                {
                    otherParty = otherParty with
                    {
                        Date = DateTimeOffset.TryParse(
                            parsedOtherParty[3],
                            out DateTimeOffset value) ? value : default,
                    };
                }

                return (true, otherParty);
            }
        }

        return (false, default);
    }

    private OtherPartyRecord ParseMemo()
    {
        if (Memo is null || Type is null)
        {
            return default;
        }

        if (Type.Equals("Comment", StringComparison.InvariantCultureIgnoreCase))
        {
            return new(
                nameof(BankTransaction.Memo),
                null,
                null,
                null,
                Memo);
        }

        (bool match, OtherPartyRecord otherParty) matchResult =
            Match(_transferFromParser, 3,
            groupArray
                => new(
                    groupArray[2].Value,
                    null,
                    null,
                    null,
                    groupArray[1].Value));

        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_transferToParser, 4, groupArray
            => new(
                groupArray[1].Value,
                null,
                null,
                DateTimeOffset.Parse(
                    groupArray[3].Value ?? "01/01/1900"),
                groupArray[2].Value)
            );

        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_debitParser, 4, groupArray
            => new(
                groupArray[1].Value,
                null,
                null,
                DateTimeOffset.Parse(
                    groupArray[2].Value ?? "01/01/1900"),
                groupArray[3].Value)
            );

        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_loanPaymentParser, 3, groupArray
            => new(
                groupArray[2].Value,
                null,
                null,
                null,
                groupArray[1].Value)
            );

        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_checkReceivedParser, 2, groupArray
            => new(
                "Check",
                null,
                null,
                null,
                groupArray[1].Value)
            );

        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_withdrawalParser, 3, groupArray
            => new(
                groupArray[1].Value,
                null,
                null,
                null,
                groupArray[2].Value)
            );

        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_creditParser, 5,  groupArray
            => new(
                groupArray[1].Value,
                null,
                null,
                DateTimeOffset.Parse(groupArray[3].Value ?? "01/01/1900"),
                $"{groupArray[2].Value} {groupArray[4].Value}")
            );

        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_changupParser, 3,
            groupArray => new(
                $"Account {groupArray[1].Value}",
                null,
                null,
                null,
                groupArray[2].Value));

        return matchResult.match
            ? matchResult.otherParty
            : new OtherPartyRecord(Memo, null, null, null, null);
    }

    public bool ApplyFilter(Regex filterExpression)
        => filterExpression.IsMatch(OtherParty);

    public bool ApplyFilter(string pattern)
        => ApplyFilter(
            new Regex(
                pattern,
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline));

    public override string ToString()
        => OtherParty;
}

public partial class BankTransaction
{
    private static List<PropertyInfo>? _columns = null;

    public static List<PropertyInfo> Columns => _columns
        ??= typeof(BankTransaction)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToList();

    public static BankTransaction[]? FromJson(string json)
        => JsonConvert.DeserializeObject<BankTransaction[]>(json, Converter.Settings);

    public static IEnumerable<BankTransaction> FromCsv(string fileName)
    {
        static decimal? ReturnNullDecimal() => null;

        static decimal? ParseDecimal(string item)
            => decimal.TryParse(item, out decimal data)
                            ? data
                            : ReturnNullDecimal();

        //var stream = File.OpenText(fileName);
        var lines = File.ReadAllLines(fileName).ToList();

        int headerRow = 0;
        for (int i=0; i < lines.Count; i++)
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

        ChoCSVLiteReader reader = new ();
        IEnumerable<string[]> rows = reader.ReadLines(lines);

        List<string> fieldNames = new();
        foreach (string[] values in rows)
        {
            BankTransaction transaction = new();
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
                    string? value = values[i]?.Trim();
                    if (currentField is not null && value is null)
                    {
                        currentField = fields.Dequeue();
                        data.Add(currentField, value);
                        continue;
                    }
                    if (currentField is not null && inQuoted &&
                        (value?.EndsWith("\"") ?? false))
                    {
                        quotedSections.Add(value.TrimEnd('\"'));
                        inQuoted = false;
                        value = String.Join(", ", quotedSections);
                        data.Add(currentField, value);
                    }
                    else if (value?.StartsWith("\"") ?? false)
                    {
                        quotedSections.Clear();
                        quotedSections.Add(value.TrimStart('\"'));
                        currentField = fields.Dequeue();
                        inQuoted = true;
                    }
                    else if (inQuoted && value is not null)
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

            transaction.TransactionNumber = data.TryGetValue("Transaction Number", out string? tr) ? tr : null;
            transaction.Description = data.TryGetValue("Description", out string? desc) ? desc : null;
            transaction.Memo = data.TryGetValue(nameof(BankTransaction.Memo), out string? memo) ? memo : null;
            transaction.Date = data.TryGetValue("Date", out string? date) ? DateOnly.Parse(date!) : null;
            transaction.AmountCredit = data.TryGetValue("Amount Credit", out string? credit) ? ParseDecimal(credit!) : null;
            transaction.AmountDebit = data.TryGetValue("Amount Debit", out string? debit) ? ParseDecimal(debit!) : null;

            yield return transaction;

            data.Clear();
        }

    }

    private static readonly Regex _changupParser = new(@"(\d+)\s(.*)", RegexOptions.Singleline);
    private static readonly Regex _checkReceivedParser = new(@"(Check Received [\,\d]+\.\d{2})", RegexOptions.Singleline);

    private static readonly Regex _creditParser = new(@"From ([a-zA-Z\s,]*)\s(.*)\s(\d\d/\d\d/\d\d\d\d\s\d\d\:\d\d)\s(.*)",
        RegexOptions.Singleline);

    private static readonly Regex _debitParser = new(@"(.*)\sDate\s(\d\d/\d\d/\d\d)\s(.*)", RegexOptions.Singleline);
    private static readonly Regex _loanPaymentParser = new(@"(Withdrawal Transfer To)\s(Loan\s\d+)", RegexOptions.Singleline);
    private static readonly Regex _transferFromParser = new(@"(Deposit Transfer From)\s(Share\s\d+)", RegexOptions.Singleline);

    private static readonly Regex _transferToParser = new(@"Transfer To\s([^\d]*)(.*)\sInternet Banking\s(.*\s\d\d\:\d\d)",
        RegexOptions.Singleline);

    private static readonly Regex _withdrawalParser = new(@"(.*)\s%%\s(.*)", RegexOptions.Singleline);
}

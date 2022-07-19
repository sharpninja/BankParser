using System;
using System.Reflection;
using System.Text.RegularExpressions;

using BankParser.Core.Models.Converters;

using ChoETL;

using CommunityToolkit.Mvvm.ComponentModel;

using Newtonsoft.Json;

// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models;

[ObservableObject]
public partial class BankTransactionView
{
    private string? _type;
    private string? _otherParty;

    private readonly ImmutableBankTransaction _trx;
    private string _notes;

    public BankTransactionView(ImmutableBankTransaction trx)
    {
        _trx = trx;
    }

    [JsonProperty("Date"), JsonConverter(typeof(DateOnlyConverter)),]
    public DateOnly? Date => _trx.Date;

    [JsonIgnore]
    public string? Type => _type ??= ParseDescription();

    [JsonIgnore]
    public string OtherParty => _otherParty ??= ParseMemo().Name;

    [JsonProperty("Amount Debit")]
    public decimal? AmountDebit => _trx.AmountDebit;

    [JsonIgnore]
    public string? AmountDebitString => AmountDebit != null
        ? AmountDebit.Value.ToString("C2")
        : null;

    [JsonProperty("Amount Credit")]
    [ChoCSVRecordField(FieldName = "Amount Credit")]
    public decimal? AmountCredit => _trx.AmountCredit;

    [JsonIgnore]
    public string? AmountCreditString => AmountCredit != null
        ? AmountCredit.Value.ToString("C2")
        : null;

    [JsonProperty("Fees")]
    public decimal? Fees => _trx.Fees;

    [JsonProperty("Index")]
    [JsonConverter(typeof(ParseStringConverter))]
    public long Index
    {
        get;
        set;
    }

    [JsonProperty("Transaction Number")]
    public string? TransactionNumber => _trx.TransactionNumber;

    [JsonProperty("Description")]
    public string? Description => _trx.Description;

    [JsonProperty(nameof(BankTransactionView.Memo))]
    public string? Memo => _trx.Memo;

    [JsonProperty(nameof(BankTransactionView.Metadata))]
    public BankTransactionMetadata Metadata
    {
        get;
        set;
    }

    [ JsonProperty ]
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    [JsonIgnore]
    public IEnumerable<string> PotentialFilters
    {
        get
        {
            var results = OtherParty
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(
                static s
                => s.Split('*', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ).ToList();

            var newResult = results.ToList();

            for (int i = 0; i < results.Count; i++)
            {
                string toFind = i is 0
                    ? results[i]
                    : ' ' + results[i];

                if (i is 0 && toFind is "PP" && OtherParty.StartsWith("PP*", StringComparison.CurrentCultureIgnoreCase))
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
                Index index = OtherParty.IndexOf(toFind, StringComparison.CurrentCultureIgnoreCase);
                int nextSpace = OtherParty.IndexOf(' ', index.Value);
                if (nextSpace > -1)
                {
                    int secondSpace = OtherParty.IndexOf(' ', nextSpace + 1);
                    if (secondSpace > -1)
                    {
                        string term = OtherParty[index..secondSpace];
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
        if (Description is (null or ""))
        {
            return null;
        }

        string[] words = Description.ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static s => s.Trim())
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
            result.Add(
                i > -1
                    ? string.Join(' ', parts[start..index])
                    : string.Join(' ', parts[start..])
            );
        }
        else if (_debitParser.IsMatch(otherParty))
        {
            (bool match, var otherPartyRecord) = Match(BankTransactionView._debitParser, 4,
                static groupArray => new(groupArray[1].Value, null, null,
                    DateTimeOffset.Parse(groupArray[2].Value), groupArray[3].Value));

            if (match && (otherPartyRecord != default))
            {
                result.Add(otherPartyRecord.Name);
                result.Add(otherPartyRecord.Address != null
                    ? otherPartyRecord.Address
                    : "");
                result.Add(otherPartyRecord.Phone != null
                    ? otherPartyRecord.Phone
                    : "");
                result.Add(otherPartyRecord.Date != null
                    ? otherPartyRecord.Date.Value.ToString()
                    : "");
            }
        }

        if ("CA TN WA KS".IndexOf(last, StringComparison.Ordinal) <= -1)
        {
            return result.ToArray();
        }

        if ((result.Any() && result[0].EndsWith(last, StringComparison.Ordinal)) ||
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
        string? toParse = Memo != null
            ? Memo
            : Description;

        if (toParse is null)
        {
            return default;
        }

        Match? matches = regex.Matches(toParse).FirstOrDefault();
        GroupCollection? groups = matches != null
            ? matches.Groups
            : null;
        if ((groups == null) || (groups.Count != expectedCount))
        {
            return (false, default);
        }

        Group[] groupArray = groups.Values.ToArray();

        OtherPartyRecord otherParty = factory(groupArray) != null
            ? factory(groupArray)!.Value
            : default;

        if (otherParty == default)
        {
            return (false, default);
        }

        string[] parsedOtherParty = ParseOtherParty(otherParty.Name);

        if (parsedOtherParty.Length > 0)
        {
            otherParty = otherParty with { Name = parsedOtherParty[0], };
        }

        if (parsedOtherParty.Length > 1)
        {
            otherParty = otherParty with { Address = parsedOtherParty[1], };
        }

        if (parsedOtherParty.Length > 2)
        {
            otherParty = otherParty with { Phone = parsedOtherParty[2], };
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

    private OtherPartyRecord ParseMemo()
    {
        if (Memo is null || Type is null)
        {
            return default;
        }

        if (Type.Equals("Comment", StringComparison.InvariantCultureIgnoreCase))
        {
            return new(
                nameof(BankTransactionView.Memo),
                null,
                null,
                null,
                Memo);
        }

        (bool match, OtherPartyRecord otherParty) matchResult =
            Match(_transferFromParser, 3,
                static groupArray
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

        matchResult = Match(_transferToParser, 4,
            static groupArray
            => new(
                groupArray[1].Value,
                null,
                null,
                DateTimeOffset.Parse(
                    groupArray[3].Value),
                groupArray[2].Value)
            );

        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_debitParser, 4,
            static groupArray
            => new(
                groupArray[1].Value,
                null,
                null,
                DateTimeOffset.Parse(
                    groupArray[2].Value),
                groupArray[3].Value)
            );

        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_loanPaymentParser, 3,
            static groupArray
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

        matchResult = Match(_checkReceivedParser, 2,
            static groupArray
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

        matchResult = Match(_withdrawalParser, 3,
            static groupArray
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

        matchResult = Match(_creditParser, 5,
            static groupArray
            => new(
                groupArray[1].Value,
                null,
                null,
                DateTimeOffset.Parse(groupArray[3].Value),
                $"{groupArray[2].Value} {groupArray[4].Value}")
            );

        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_changupParser, 3,
            static groupArray => new(
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

public partial class BankTransactionView
{
    private static List<PropertyInfo>? _columns;

    public static List<PropertyInfo> Columns => _columns
        ??= typeof(BankTransactionView)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToList();

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

    public static List<BankTransactionView> FromImmutable(IEnumerable<ImmutableBankTransaction> unfiltered)
        => unfiltered.Select(static trx => new BankTransactionView(trx)).ToList();
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models;

public class BankTransaction
{
    private readonly Regex _changupParser = new(@"(\d+)\s(.*)", RegexOptions.Singleline);
    private readonly Regex _checkReceivedParser = new(@"(Check Received [\,\d]+\.\d{2})", RegexOptions.Singleline);

    private readonly Regex _creditParser = new(@"From ([a-zA-Z\s,]*)\s(.*)\s(\d\d/\d\d/\d\d\d\d\s\d\d\:\d\d)\s(.*)",
        RegexOptions.Singleline);

    private readonly Regex _debitParser = new(@"(.*)\sDate\s(\d\d/\d\d/\d\d)\s(.*)", RegexOptions.Singleline);
    private readonly Regex _loanPaymentParser = new(@"(Withdrawal Transfer To)\s(Loan\s\d+)", RegexOptions.Singleline);
    private readonly Regex _transferFromParser = new(@"(Deposit Transfer From)\s(Share\s\d+)", RegexOptions.Singleline);

    private readonly Regex _transferToParser = new(@"Transfer To\s([^\d]*)(.*)\sInternet Banking\s(.*\s\d\d\:\d\d)",
        RegexOptions.Singleline);

    private readonly Regex _withdrawalParser = new(@"(.*)\s%%\s(.*)", RegexOptions.Singleline);

    [JsonProperty("Date")]
    public DateTimeOffset Date
    {
        get;
        set;
    }

    public string Type => ParseDescription();

    public string OtherParty => ParseMemo().Name;

    [JsonProperty("Amount Debit")]
    public decimal? AmountDebit
    {
        get;
        set;
    }

    [JsonProperty("Amount Credit")]
    public decimal? AmountCredit
    {
        get;
        set;
    }

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
    public string TransactionNumber
    {
        get;
        set;
    }

    [JsonProperty("Description")]
    public string Description
    {
        get;
        set;
    }

    [JsonProperty("Memo")]
    public string Memo
    {
        get;
        set;
    }

    private string ParseDescription()
    {
        string[] words = Description.ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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
            _ => $"UNKOWN: [{Description}]"
        };
    }

    private string[] ParseOtherParty(string otherParty)
    {
        string[] parts = otherParty.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<string> result = new ();

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
            if (matchResult.match)
            {
                result.Add(matchResult.otherParty.Name);
                result.Add(matchResult.otherParty.Address);
                result.Add(matchResult.otherParty.Phone);
                result.Add(matchResult.otherParty.Date.ToString());
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

    private (bool match, OtherPartyRecord otherParty) Match(Regex regex, int expectedCount,
        Func<Group[], OtherPartyRecord> factory)
    {
        Match matches = regex.Matches(Memo).FirstOrDefault();
        GroupCollection groups = matches?.Groups;
        if (groups?.Count != expectedCount)
        {
            return (false, null);
        }

        Group[] groupArray = groups.Values.ToArray();
        OtherPartyRecord otherParty = factory.Invoke(groupArray);
        string[] parsedOtherParty = ParseOtherParty(otherParty.Name);

        if (parsedOtherParty.Length > 0)
        {
            otherParty = otherParty with { Name = parsedOtherParty[0] };
        }

        if (parsedOtherParty.Length > 1)
        {
            otherParty = otherParty with { Address = parsedOtherParty[1] };
        }

        if (parsedOtherParty.Length > 2)
        {
            otherParty = otherParty with { Phone = parsedOtherParty[2] };
        }

        if (parsedOtherParty.Length > 3)
        {
            otherParty = otherParty with
            {
                Date = DateTimeOffset.TryParse(parsedOtherParty[3], out DateTimeOffset value) ? value : default
            };
        }

        return (true, otherParty);

    }

    private OtherPartyRecord ParseMemo()
    {
        if (Memo is null or "")
        {
            Memo = Description;
        }

        if (Type.Equals("Comment", StringComparison.InvariantCultureIgnoreCase))
        {
            return new("Memo", null, null, null, Memo);
        }

        (bool match, OtherPartyRecord otherParty) matchResult = Match(_transferFromParser, 3,
            groupArray => new(groupArray[2].Value, null, null, null, groupArray[1].Value));
        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_transferToParser, 4,
            groupArray => new(groupArray[1].Value, null, null,
                DateTimeOffset.Parse(groupArray[3].Value ?? "01/01/1900"), groupArray[2].Value));
        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_debitParser, 4,
            groupArray => new(groupArray[1].Value, null, null,
                DateTimeOffset.Parse(groupArray[2].Value ?? "01/01/1900"), groupArray[3].Value));
        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_loanPaymentParser, 3,
            groupArray => new(groupArray[2].Value, null, null, null, groupArray[1].Value));
        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_checkReceivedParser, 2, groupArray => new("Check", null, null, null, groupArray[1].Value));
        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_withdrawalParser, 3,
            groupArray => new(groupArray[1].Value, null, null, null, groupArray[2].Value));
        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_creditParser, 5,
            groupArray => new(groupArray[1].Value, null, null,
                DateTimeOffset.Parse(groupArray[3].Value ?? "01/01/1900"),
                $"{groupArray[2].Value} {groupArray[4].Value}"));
        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        matchResult = Match(_changupParser, 3,
            groupArray => new($"Account {groupArray[1].Value}", null, null, null, groupArray[2].Value));
        if (matchResult.match)
        {
            return matchResult.otherParty;
        }

        return new OtherPartyRecord(Memo, null, null, null, null);
    }

    public record OtherPartyRecord(string Name, string Address, string Phone, DateTimeOffset? Date, string Other);

    public static BankTransaction[] FromJson(string json) =>
        JsonConvert.DeserializeObject<BankTransaction[]>(json, Converter.Settings);
}
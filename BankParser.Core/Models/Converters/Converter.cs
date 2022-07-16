using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BankParser.Core.Models.Converters;

internal static class Converter
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal,},
            new ParseStringConverter(),
        },
    };
}
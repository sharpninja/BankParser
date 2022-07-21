namespace BankParser.Core.Models.Converters;

using System.Globalization;

using Newtonsoft.Json.Converters;

internal static class Converter
{
    public static readonly JsonSerializerSettings _settings = new()
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
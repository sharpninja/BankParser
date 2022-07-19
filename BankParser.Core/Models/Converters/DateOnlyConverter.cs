using System;

using Newtonsoft.Json;
// ReSharper disable SuggestVarOrType_BuiltInTypes

namespace BankParser.Core.Models.Converters;

public class DateOnlyConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
        => (objectType == typeof(DateTimeOffset)) || (objectType == typeof(DateTime));
    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var deserialized = serializer.Deserialize<DateTimeOffset>(reader);
        return DateOnly.FromDateTime(deserialized.DateTime);
    }
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        => serializer.Serialize(writer, value);
}


using Newtonsoft.Json;

namespace BankParser.Core.Models;

public static class Serialize
{
    public static string ToJson(this BankTransaction[] self) =>
        JsonConvert.SerializeObject(self, Converter.Settings);
}
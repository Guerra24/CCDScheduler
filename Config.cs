using DotNet.Globbing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CCDScheduler;

public class ProcessRule
{
    public List<string> Selectors { get; set; } = null!;
    [JsonConverter(typeof(HexStringJsonConverter))]
    public nint Affinity { get; set; }
    public bool Force { get; set; }
    public int Delay { get; set; }
}

public class Config
{
    public List<ProcessRule> ProcessRules { get; set; } = null!;
}


public static class JsonSettings
{
    public static JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true,
        WriteIndented = true,
        TypeInfoResolver = JsonSourceGenerationContext.Default,
    };
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, NumberHandling = JsonNumberHandling.AllowReadingFromString, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, IncludeFields = true, WriteIndented = true)]
[JsonSerializable(typeof(ProcessRule))]
[JsonSerializable(typeof(Config))]
public partial class JsonSourceGenerationContext : JsonSerializerContext
{

}

public record MatcherAffinity(Glob Glob, nint Affinity, int Delay);
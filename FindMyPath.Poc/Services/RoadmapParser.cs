using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Services;

/// <summary>Tolerant extraction of the roadmap JSON from the model's response. Returns null if it can't parse.</summary>
public static class RoadmapParser
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true,
        // Be forgiving of small, realistic model deviations rather than dumping raw JSON at the client:
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new TolerantStringConverter() },
    };

    public static RoadmapDto? TryParse(string text)
    {
        var json = ExtractJson(text);
        if (json is null) return null;
        try
        {
            var dto = JsonSerializer.Deserialize<RoadmapDto>(json, Opts);
            if (dto is null) return null;

            // The model can legitimately emit explicit `null` for a collection (e.g. "phases": null);
            // System.Text.Json would leave those null and the render would throw. Normalize defensively.
            dto.Phases ??= new();
            dto.Notes ??= new();
            foreach (var p in dto.Phases) p.Steps ??= new();

            if (!string.IsNullOrWhiteSpace(dto.Summary) || dto.Phases.Count > 0 || !string.IsNullOrWhiteSpace(dto.RecommendedPathway))
            {
                return dto;
            }
        }
        catch
        {
            // fall through - render raw text instead
        }
        return null;
    }

    private static string? ExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // 1) A fenced ```json ... ``` (or plain ``` ... ```) block.
        var fence = Regex.Match(text, "```(?:json)?\\s*(\\{.*\\})\\s*```", RegexOptions.Singleline);
        if (fence.Success) return fence.Groups[1].Value;

        // 2) The first '{' spanning to the last '}'.
        int first = text.IndexOf('{');
        int last = text.LastIndexOf('}');
        if (first >= 0 && last > first) return text.Substring(first, last - first + 1);

        return null;
    }
}

/// <summary>
/// Reads JSON strings, but also tolerates numbers/booleans/null (and skips objects/arrays) in a
/// string-typed field — so a model emitting e.g. <c>"estimatedCost": 4500</c> instead of a quoted
/// string does not fail the entire document and dump raw JSON at the client during a live demo.
/// </summary>
internal sealed class TolerantStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var l)) return l.ToString(CultureInfo.InvariantCulture);
                if (reader.TryGetDecimal(out var m)) return m.ToString(CultureInfo.InvariantCulture);
                if (reader.TryGetDouble(out var d)) return d.ToString(CultureInfo.InvariantCulture);
                return null;
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            default:
                // Objects/arrays where a string was expected: skip the whole token rather than throw.
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}

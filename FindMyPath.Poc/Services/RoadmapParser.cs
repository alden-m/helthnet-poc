using System.Text.Json;
using System.Text.RegularExpressions;
using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Services;

/// <summary>Tolerant extraction of the roadmap JSON from the model's response. Returns null if it can't parse.</summary>
public static class RoadmapParser
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static RoadmapDto? TryParse(string text)
    {
        var json = ExtractJson(text);
        if (json is null) return null;
        try
        {
            var dto = JsonSerializer.Deserialize<RoadmapDto>(json, Opts);
            if (dto is not null &&
                (!string.IsNullOrWhiteSpace(dto.Summary) || dto.Phases.Count > 0 || !string.IsNullOrWhiteSpace(dto.RecommendedPathway)))
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

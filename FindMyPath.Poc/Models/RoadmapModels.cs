using System.Text.Json.Serialization;

namespace FindMyPath.Poc.Models;

/// <summary>The AI's roadmap, enforced by the Anthropic structured-output schema.</summary>
public class RoadmapDto
{
    [JsonPropertyName("summary")] public string Summary { get; set; } = "";
    [JsonPropertyName("recommendedPathway")] public string RecommendedPathway { get; set; } = "";
    [JsonPropertyName("estimatedTotalTimeline")] public string EstimatedTotalTimeline { get; set; } = "";
    [JsonPropertyName("estimatedTotalCost")] public string EstimatedTotalCost { get; set; } = "";
    [JsonPropertyName("phases")] public List<PhaseDto> Phases { get; set; } = new();
    [JsonPropertyName("notes")] public List<string> Notes { get; set; } = new();
}

public class PhaseDto
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("steps")] public List<StepDto> Steps { get; set; } = new();
}

public class StepDto
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("estimatedTimeline")] public string EstimatedTimeline { get; set; } = "";
    [JsonPropertyName("estimatedCost")] public string EstimatedCost { get; set; } = "";
}

public class TokenUsage
{
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long CacheReadTokens { get; set; }
    public long CacheWriteTokens { get; set; }
    public long TotalTokens => InputTokens + OutputTokens + CacheReadTokens + CacheWriteTokens;
}

/// <summary>Result of one generation call: raw text, parsed roadmap (or null), token usage, and cost.</summary>
public class RoadmapResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string RawText { get; set; } = "";
    public RoadmapDto? Roadmap { get; set; } // null when the response wasn't valid JSON
    public TokenUsage Usage { get; set; } = new();
    public decimal CostUsd { get; set; }
    public string Model { get; set; } = "";
    public string Effort { get; set; } = GenerationTuningCatalog.DefaultEffort;
    public int MaxOutputTokens { get; set; } = GenerationTuningCatalog.DefaultMaxOutputTokens;
    public bool IncludeKnowledgeBase { get; set; }
    public string QuestionnaireVersion { get; set; } = AssessmentAnswers.QuestionnaireVersion;
    public string GeneratedAtUtc { get; set; } = "";
    public bool ParsedOk => Roadmap is not null;

    // Exactly what was sent to the AI (for the "what was sent" panel and history snapshot).
    // SystemInstruction here is the editable/visible instruction only — the mandatory JSON-output
    // contract is appended internally at request time and deliberately not surfaced.
    public string SystemInstruction { get; set; } = "";
    public string UserMessage { get; set; } = "";

    /// <summary>Knowledge-base files attached to this request (name, kind, whether they were sent).</summary>
    public List<AttachmentInfo> Attachments { get; set; } = new();
}

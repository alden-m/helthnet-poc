namespace FindMyPath.Poc.Models;

/// <summary>The AI's roadmap, matching the JSON contract in the system instruction.</summary>
public class RoadmapDto
{
    public string? Summary { get; set; }
    public string? RecommendedPathway { get; set; }
    public string? EstimatedTotalTimeline { get; set; }
    public string? EstimatedTotalCost { get; set; }
    public List<PhaseDto> Phases { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

public class PhaseDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<StepDto> Steps { get; set; } = new();
}

public class StepDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? EstimatedTimeline { get; set; }
    public string? EstimatedCost { get; set; }
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
    public bool ParsedOk => Roadmap is not null;

    // Exactly what was sent to the AI (for the "what was sent" panel and history snapshot).
    public string SystemInstruction { get; set; } = "";
    public string UserMessage { get; set; } = "";
    public string? ReferenceMaterial { get; set; }
}

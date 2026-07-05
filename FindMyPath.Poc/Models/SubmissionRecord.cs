namespace FindMyPath.Poc.Models;

/// <summary>
/// A full snapshot of one "Generate my pathway" run, persisted as its own JSON file under the
/// app-data history folder: the input answers, the categorized output phases, the API cost,
/// the exact prompt used, and the model.
/// </summary>
public class SubmissionRecord
{
    public string Id { get; set; } = "";
    public string TimestampUtc { get; set; } = "";
    public string Model { get; set; } = "";

    // Input
    public AssessmentAnswers Input { get; set; } = new();
    public string InputText { get; set; } = ""; // the readable Q&A block actually sent to the AI

    // Prompt snapshot
    public string SystemInstruction { get; set; } = "";
    public string? ReferenceMaterial { get; set; }

    // Output
    public RoadmapDto? Output { get; set; } // categorized phases, each with sub-steps
    public string RawOutput { get; set; } = "";
    public bool ParsedOk { get; set; }

    // Cost
    public TokenUsage Usage { get; set; } = new();
    public decimal CostUsd { get; set; }

    // Convenience for the history list
    public string? Profession { get; set; }
    public string? RecommendedPathway { get; set; }
}

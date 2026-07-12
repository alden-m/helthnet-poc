using System.Text.Json;
using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Persists and reads per-submission JSON snapshots under the project-relative <c>app_data/history</c> folder.
/// Never crashes on I/O (best-effort persistence).
/// </summary>
public class HistoryService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly AppPaths _paths;

    public HistoryService(AppPaths paths) => _paths = paths;

    public bool Save(SubmissionRecord record)
    {
        string? tempPath = null;
        try
        {
            if (record is null || !IsValidId(record.Id)) return false;

            _paths.EnsureDirs();
            var finalPath = Path.Combine(_paths.HistoryDir, $"{record.Id}.json");
            tempPath = Path.Combine(_paths.HistoryDir, $".{record.Id}.{Guid.NewGuid():N}.tmp");

            // The temporary file lives beside the destination so the final rename is atomic.
            // File.Move deliberately does not overwrite: duplicate IDs fail safely instead of
            // replacing an earlier submission.
            File.WriteAllText(tempPath, JsonSerializer.Serialize(record, JsonOpts));
            File.Move(tempPath, finalPath);
            tempPath = null;
            return true;
        }
        catch
        {
            // history is best-effort; never block a generation
            return false;
        }
        finally
        {
            if (tempPath is not null)
            {
                try { File.Delete(tempPath); }
                catch { /* best-effort cleanup */ }
            }
        }
    }

    public List<SubmissionRecord> List()
    {
        var list = new List<SubmissionRecord>();
        try
        {
            _paths.EnsureDirs();
            foreach (var file in Directory.GetFiles(_paths.HistoryDir, "*.json"))
            {
                var expectedId = Path.GetFileNameWithoutExtension(file);
                if (!IsValidId(expectedId)) continue;

                var rec = ReadAndNormalize(file, expectedId);
                if (rec is not null) list.Add(rec);
            }
        }
        catch
        {
            // return whatever we have
        }
        return list.OrderByDescending(r => r.TimestampUtc).ToList();
    }

    public SubmissionRecord? Get(string? id)
    {
        try
        {
            if (!IsValidId(id)) return null;

            var path = Path.Combine(_paths.HistoryDir, $"{id}.json");
            if (File.Exists(path))
                return ReadAndNormalize(path, id!);
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private static SubmissionRecord? ReadAndNormalize(string path, string expectedId)
    {
        try
        {
            var record = JsonSerializer.Deserialize<SubmissionRecord>(File.ReadAllText(path), JsonOpts);
            if (record is null) return null;

            record.Id ??= "";
            if (!IsValidId(record.Id) || !string.Equals(record.Id, expectedId, StringComparison.Ordinal))
                return null;

            record.TimestampUtc ??= "";
            record.Model ??= "";
            record.Effort = GenerationTuningCatalog.NormalizeEffort(record.Effort);
            record.MaxOutputTokens = GenerationTuningCatalog.NormalizeMaxOutputTokens(record.MaxOutputTokens);
            record.QuestionnaireVersion = string.IsNullOrWhiteSpace(record.QuestionnaireVersion)
                ? AssessmentAnswers.QuestionnaireVersion
                : record.QuestionnaireVersion;
            record.InputText ??= "";
            record.SystemInstruction ??= "";
            // Older snapshots predate the editable field but always used this same built-in style.
            record.OutputStyleInstruction = string.IsNullOrWhiteSpace(record.OutputStyleInstruction)
                ? DefaultPrompt.OutputStyleInstruction
                : record.OutputStyleInstruction;
            record.RawOutput ??= "";

            record.Input ??= new AssessmentAnswers();
            record.Input.ExamsCompleted = record.Input.ExamsCompleted?.OfType<string>().ToList() ?? new();
            record.Input.Goals = record.Input.Goals?.OfType<string>().ToList() ?? new();
            record.Input.LearningNeeds = record.Input.LearningNeeds?.OfType<string>().ToList() ?? new();

            record.Usage ??= new TokenUsage();
            record.Attachments = record.Attachments?.OfType<AttachmentInfo>().ToList() ?? new();
            foreach (var attachment in record.Attachments)
            {
                attachment.FileName ??= "";
                attachment.Kind ??= "";
            }

            if (record.Output is not null)
            {
                record.Output.Summary ??= "";
                record.Output.RecommendedPathway ??= "";
                record.Output.EstimatedTotalTimeline ??= "";
                record.Output.EstimatedTotalCost ??= "";
                record.Output.Phases = record.Output.Phases?.OfType<PhaseDto>().ToList() ?? new();
                record.Output.Notes = record.Output.Notes?.OfType<string>().ToList() ?? new();

                foreach (var phase in record.Output.Phases)
                {
                    phase.Title ??= "";
                    phase.Description ??= "";
                    phase.Steps = phase.Steps?.OfType<StepDto>().ToList() ?? new();

                    foreach (var step in phase.Steps)
                    {
                        step.Title ??= "";
                        step.Description ??= "";
                        step.EstimatedTimeline ??= "";
                        step.EstimatedCost ??= "";
                    }
                }
            }

            return record;
        }
        catch
        {
            // Invalid JSON or an incompatible legacy snapshot is simply omitted.
            return null;
        }
    }

    private static bool IsValidId(string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 128) return false;

        foreach (var c in id)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_') return false;
        }

        return true;
    }
}

using System.Text.Json;
using FindMyPath.Poc.Models;
using FindMyPath.Poc.Services;
using FindMyPath.Poc.Tests.TestInfrastructure;

namespace FindMyPath.Poc.Tests.Services;

public class HistoryServiceTests
{
    [Fact]
    public void SaveListAndGetRoundTripCompleteSubmissionSnapshotsNewestFirst()
    {
        using var env = new TempAppEnvironment();
        var service = new HistoryService(env.CreatePaths());
        var older = CreateRecord("older", "2026-01-01T12:00:00.0000000Z", "Nurse");
        var newer = CreateRecord("newer", "2026-07-12T12:00:00.0000000Z", "Physician");

        Assert.True(service.Save(older));
        Assert.True(service.Save(newer));

        var records = service.List();
        Assert.Equal(["newer", "older"], records.Select(r => r.Id));

        var loaded = service.Get("newer");
        Assert.NotNull(loaded);
        Assert.Equal("Physician", loaded.Profession);
        Assert.Equal("Physician", loaded.Input.Profession);
        Assert.Equal("Recommended Physician pathway", loaded.RecommendedPathway);
        Assert.Equal("Recommended Physician pathway", loaded.Output?.RecommendedPathway);
        Assert.Equal(AssessmentAnswers.QuestionnaireVersion, loaded.QuestionnaireVersion);
        Assert.Equal(123, loaded.Usage.InputTokens);
        Assert.Equal(0.42m, loaded.CostUsd);
        Assert.Single(loaded.Attachments);
        Assert.Equal("guide.md", loaded.Attachments[0].FileName);

        Assert.Null(service.Get("missing"));
    }

    [Fact]
    public void ListAndGetIgnoreMalformedAndJsonNullSnapshots()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        var service = new HistoryService(paths);
        service.Save(CreateRecord("valid", "2026-07-12T12:00:00.0000000Z", "Nurse"));
        File.WriteAllText(Path.Combine(paths.HistoryDir, "malformed.json"), "{ definitely-not-json");
        File.WriteAllText(Path.Combine(paths.HistoryDir, "null.json"), "null");

        var records = service.List();

        Assert.Single(records);
        Assert.Equal("valid", records[0].Id);
        Assert.Null(service.Get("malformed"));
        Assert.Null(service.Get("null"));
    }

    [Fact]
    public void LegacySnapshotsNormalizeNullObjectsCollectionsAndStrings()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        paths.EnsureDirs();
        const string legacyJson = """
            {
              "id": "legacy",
              "timestampUtc": "2025-01-01T00:00:00Z",
              "model": null,
              "input": {
                "profession": "Nurse",
                "examsCompleted": null,
                "goals": null,
                "learningNeeds": null
              },
              "inputText": null,
              "systemInstruction": null,
              "attachments": null,
              "output": {
                "summary": "Legacy roadmap",
                "phases": [
                  { "title": "Phase", "steps": null }
                ],
                "notes": null
              },
              "rawOutput": null,
              "usage": null
            }
            """;
        File.WriteAllText(Path.Combine(paths.HistoryDir, "legacy.json"), legacyJson);

        var loaded = new HistoryService(paths).Get("legacy");

        Assert.NotNull(loaded);
        Assert.NotNull(loaded.Model);
        Assert.NotNull(loaded.Input);
        Assert.NotNull(loaded.Input.ExamsCompleted);
        Assert.NotNull(loaded.Input.Goals);
        Assert.NotNull(loaded.Input.LearningNeeds);
        Assert.NotNull(loaded.InputText);
        Assert.NotNull(loaded.SystemInstruction);
        Assert.NotNull(loaded.Attachments);
        Assert.NotNull(loaded.RawOutput);
        Assert.NotNull(loaded.Usage);
        Assert.NotNull(loaded.Output);
        Assert.NotNull(loaded.Output.Phases);
        Assert.NotNull(loaded.Output.Notes);
        Assert.NotNull(Assert.Single(loaded.Output.Phases).Steps);
    }

    [Fact]
    public void LegacySnapshotWithNullInputGetsAnEmptySafeAnswerModel()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        paths.EnsureDirs();
        File.WriteAllText(
            Path.Combine(paths.HistoryDir, "null-input.json"),
            """{"id":"null-input","timestampUtc":"2025-01-01T00:00:00Z","input":null}""");

        var loaded = new HistoryService(paths).Get("null-input");

        Assert.NotNull(loaded);
        Assert.NotNull(loaded.Input);
        Assert.NotNull(loaded.Input.ExamsCompleted);
        Assert.NotNull(loaded.Input.Goals);
        Assert.NotNull(loaded.Input.LearningNeeds);
    }

    [Fact]
    public void GetRejectsPathTraversalEvenWhenAValidSnapshotExistsOutsideHistory()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        paths.EnsureDirs();
        var outsidePath = Path.Combine(paths.DataDir, "outside.json");
        File.WriteAllText(outsidePath, JsonSerializer.Serialize(
            CreateRecord("outside", "2026-07-12T12:00:00Z", "Nurse")));

        var loaded = new HistoryService(paths).Get("../outside");

        Assert.Null(loaded);
        Assert.True(File.Exists(outsidePath));
    }

    [Fact]
    public void SaveNeverWritesAPathTraversalIdOutsideHistory()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        var service = new HistoryService(paths);
        var record = CreateRecord("../escaped-save", "2026-07-12T12:00:00Z", "Nurse");

        var saved = service.Save(record);

        Assert.False(saved);
        Assert.False(File.Exists(Path.Combine(paths.DataDir, "escaped-save.json")));
    }

    [Fact]
    public async Task ConcurrentSavesWithCallerSuppliedUniqueIdsPreserveEverySnapshot()
    {
        using var env = new TempAppEnvironment();
        var service = new HistoryService(env.CreatePaths());
        const int count = 32;

        await Task.WhenAll(Enumerable.Range(0, count).Select(i => Task.Run(() =>
            service.Save(CreateRecord($"parallel-{i:D2}", $"2026-07-12T12:00:{i:D2}Z", "Nurse")))));

        var records = service.List();
        Assert.Equal(count, records.Count);
        Assert.Equal(count, records.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task ConcurrentSavesWithTheSameIdNeverOverwriteAndExactlyOneWins()
    {
        using var env = new TempAppEnvironment();
        var service = new HistoryService(env.CreatePaths());
        const int count = 16;

        var results = await Task.WhenAll(Enumerable.Range(0, count).Select(i => Task.Run(() =>
            service.Save(CreateRecord("same-id", $"2026-07-12T12:00:{i:D2}Z", $"Profession {i}")))));

        Assert.Single(results, saved => saved);
        Assert.Single(service.List());
        Assert.NotNull(service.Get("same-id"));
    }

    private static SubmissionRecord CreateRecord(string id, string timestamp, string profession) => new()
    {
        Id = id,
        TimestampUtc = timestamp,
        Model = ModelCatalog.DefaultModel,
        Input = new AssessmentAnswers
        {
            Profession = profession,
            ExamsCompleted = ["None"],
            Goals = ["Obtain professional licence"],
            LearningNeeds = ["Exam Preparation"],
        },
        InputText = "Questionnaire input",
        SystemInstruction = "System prompt",
        Attachments =
        [
            new AttachmentInfo { FileName = "guide.md", Bytes = 12, Kind = "text", Sent = true },
        ],
        Output = new RoadmapDto
        {
            Summary = "Summary",
            RecommendedPathway = $"Recommended {profession} pathway",
            Phases =
            [
                new PhaseDto
                {
                    Title = "Phase one",
                    Steps = [new StepDto { Title = "First step" }],
                },
            ],
        },
        RawOutput = "{ }",
        ParsedOk = true,
        Usage = new TokenUsage { InputTokens = 123, OutputTokens = 456 },
        CostUsd = 0.42m,
        Profession = profession,
        RecommendedPathway = $"Recommended {profession} pathway",
    };
}

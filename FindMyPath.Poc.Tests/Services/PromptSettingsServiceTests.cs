using FindMyPath.Poc.Models;
using FindMyPath.Poc.Services;
using FindMyPath.Poc.Tests.TestInfrastructure;
using Microsoft.Extensions.Configuration;

namespace FindMyPath.Poc.Tests.Services;

public class PromptSettingsServiceTests
{
    [Fact]
    public void MissingSettingsFileUsesSafeDemoDefaults()
    {
        using var env = new TempAppEnvironment();

        var service = CreateService(env.CreatePaths());
        var settings = service.Current;

        Assert.Equal(ModelCatalog.DefaultModel, settings.Model);
        Assert.Equal(DefaultPrompt.SystemInstruction, settings.SystemInstruction);
        Assert.False(settings.IncludeKnowledgeBase);
        Assert.Equal("high", settings.Effort);
        Assert.Equal(8192, settings.MaxOutputTokens);
    }

    [Fact]
    public void SavePersistsEveryTuningFieldAndASecondServiceReloadsThem()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        var first = CreateService(paths);

        var saved = first.Save(
            "A custom system instruction",
            "claude-sonnet-5",
            includeKnowledgeBase: true,
            effort: "medium",
            maxOutputTokens: 4096);
        var reloaded = CreateService(paths).Current;

        Assert.True(saved);
        Assert.Equal("A custom system instruction", reloaded.SystemInstruction);
        Assert.Equal("claude-sonnet-5", reloaded.Model);
        Assert.True(reloaded.IncludeKnowledgeBase);
        Assert.Equal("medium", reloaded.Effort);
        Assert.Equal(4096, reloaded.MaxOutputTokens);
        Assert.True(File.Exists(paths.SettingsFile));
    }

    [Fact]
    public void LegacySettingsWithoutTuningFieldsReceiveTheNewDefaults()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        paths.EnsureDirs();
        File.WriteAllText(
            paths.SettingsFile,
            """{"model":"claude-haiku-4-5","systemInstruction":"Legacy prompt"}""");

        var settings = CreateService(paths).Current;

        Assert.Equal("claude-haiku-4-5", settings.Model);
        Assert.Equal("Legacy prompt", settings.SystemInstruction);
        Assert.False(settings.IncludeKnowledgeBase);
        Assert.Equal("high", settings.Effort);
        Assert.Equal(8192, settings.MaxOutputTokens);
    }

    [Fact]
    public void CorruptSettingsFileFallsBackWithoutThrowing()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        paths.EnsureDirs();
        File.WriteAllText(paths.SettingsFile, "{ this-is-not-json");

        var service = CreateService(paths);
        var settings = service.Current;

        Assert.Equal(ModelCatalog.DefaultModel, settings.Model);
        Assert.Equal(DefaultPrompt.SystemInstruction, settings.SystemInstruction);
        Assert.False(settings.IncludeKnowledgeBase);
        Assert.Equal("high", settings.Effort);
        Assert.Equal(8192, settings.MaxOutputTokens);
    }

    [Fact]
    public void BlankPromptAndModelResetToDefaultsWhileKeepingExplicitTuning()
    {
        using var env = new TempAppEnvironment();
        var service = CreateService(env.CreatePaths());

        var saved = service.Save(
            " \t ",
            " ",
            includeKnowledgeBase: true,
            effort: "low",
            maxOutputTokens: 4096);
        var settings = service.Current;

        Assert.True(saved);
        Assert.Equal(ModelCatalog.DefaultModel, settings.Model);
        Assert.Equal(DefaultPrompt.SystemInstruction, settings.SystemInstruction);
        Assert.True(settings.IncludeKnowledgeBase);
        Assert.Equal("low", settings.Effort);
        Assert.Equal(4096, settings.MaxOutputTokens);
    }

    [Fact]
    public void ResetSystemInstructionDoesNotResetOtherTuningControls()
    {
        using var env = new TempAppEnvironment();
        var service = CreateService(env.CreatePaths());
        Assert.True(service.Save("Custom", "claude-haiku-4-5", true, "medium", 4096));

        var reset = service.ResetSystemInstruction();
        var settings = service.Current;

        Assert.Equal(DefaultPrompt.SystemInstruction, reset);
        Assert.Equal(DefaultPrompt.SystemInstruction, settings.SystemInstruction);
        Assert.Equal("claude-haiku-4-5", settings.Model);
        Assert.True(settings.IncludeKnowledgeBase);
        Assert.Equal("medium", settings.Effort);
        Assert.Equal(4096, settings.MaxOutputTokens);
    }

    [Fact]
    public void ApiKeyComesFromConfigurationAndIsNeverWrittenToSettingsJson()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Anthropic:ApiKey"] = "test-secret-key",
            })
            .Build();
        var service = new PromptSettingsService(configuration, paths);

        Assert.Equal("test-secret-key", service.ApiKey);
        Assert.True(service.HasApiKey);
        Assert.True(service.Save("Prompt", ModelCatalog.DefaultModel, false, "high", 8192));
        Assert.DoesNotContain("test-secret-key", File.ReadAllText(paths.SettingsFile), StringComparison.Ordinal);
    }

    private static PromptSettingsService CreateService(AppPaths paths) =>
        new(new ConfigurationBuilder().Build(), paths);
}

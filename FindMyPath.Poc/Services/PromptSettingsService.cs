using System.Text.Json;
using FindMyPath.Poc.Models;
using Microsoft.Extensions.Configuration;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Runtime tuning (system instruction, model, reference-file mode, effort, and output cap) persisted to the project-relative
/// <c>app_data/settings.json</c>. Defaults come from code — <see cref="DefaultPrompt.SystemInstruction"/> and
/// <see cref="ModelCatalog.DefaultModel"/> — not from appsettings.json. The only thing read from configuration
/// is the API key (Anthropic:ApiKey, or the ANTHROPIC_API_KEY env var); it is never written to app_data.
/// Never crashes on I/O.
/// </summary>
public class PromptSettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly object _lock = new();
    private readonly IConfiguration _config;
    private readonly AppPaths _paths;
    private readonly ILogger<PromptSettingsService>? _logger;
    private PromptSettings _settings;

    public PromptSettingsService(IConfiguration config, AppPaths paths, ILogger<PromptSettingsService>? logger = null)
    {
        _config = config;
        _paths = paths;
        _logger = logger;
        _settings = Load();
    }

    public PromptSettings Current
    {
        get { lock (_lock) { return Clone(_settings); } }
    }

    /// <summary>API key from configuration (Anthropic:ApiKey), falling back to the ANTHROPIC_API_KEY env var.</summary>
    public string? ApiKey
    {
        get
        {
            var fromConfig = _config["Anthropic:ApiKey"];
            if (!string.IsNullOrWhiteSpace(fromConfig)) return fromConfig;
            return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        }
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public bool Save(string systemInstruction, string model, bool includeKnowledgeBase,
        string effort, int maxOutputTokens)
    {
        lock (_lock)
        {
            _settings.SystemInstruction = string.IsNullOrWhiteSpace(systemInstruction)
                ? DefaultSystem()
                : StripMandatoryFormat(systemInstruction);
            _settings.Model = NormalizeModel(model);
            _settings.IncludeKnowledgeBase = includeKnowledgeBase;
            _settings.Effort = GenerationTuningCatalog.NormalizeEffort(effort);
            _settings.MaxOutputTokens = GenerationTuningCatalog.NormalizeMaxOutputTokens(maxOutputTokens);
            return Persist();
        }
    }

    public string ResetSystemInstruction()
    {
        lock (_lock)
        {
            _settings.SystemInstruction = DefaultSystem();
            Persist();
            return _settings.SystemInstruction;
        }
    }

    // Defaults are code, not configuration: appsettings.json holds only the API key.
    private static string DefaultModel() => ModelCatalog.DefaultModel;

    private static string NormalizeModel(string? model) =>
        ModelCatalog.Models.Any(m => string.Equals(m.Id, model, StringComparison.Ordinal))
            ? model!
            : DefaultModel();

    private static string DefaultSystem() => DefaultPrompt.SystemInstruction;

    /// <summary>
    /// Removes the mandatory JSON-output contract from an instruction if a legacy settings.json still
    /// carries it inline, so it never resurfaces in the editable Settings textarea. The contract is
    /// re-appended at request time by <see cref="RoadmapService"/>.
    /// </summary>
    private static string StripMandatoryFormat(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var idx = s.IndexOf("Output format:", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && s.IndexOf("single fenced JSON", StringComparison.OrdinalIgnoreCase) > idx)
            return s[..idx].TrimEnd();
        return s;
    }

    private PromptSettings Load()
    {
        try
        {
            _paths.EnsureDirs();
            if (File.Exists(_paths.SettingsFile))
            {
                var s = JsonSerializer.Deserialize<PromptSettings>(File.ReadAllText(_paths.SettingsFile), JsonOpts);
                if (s is not null)
                {
                    s.SystemInstruction = string.IsNullOrWhiteSpace(s.SystemInstruction)
                        ? DefaultSystem()
                        : StripMandatoryFormat(s.SystemInstruction);
                    s.Model = NormalizeModel(s.Model);
                    s.Effort = GenerationTuningCatalog.NormalizeEffort(s.Effort);
                    s.MaxOutputTokens = GenerationTuningCatalog.NormalizeMaxOutputTokens(s.MaxOutputTokens);
                    return s;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not load prompt settings; using safe defaults.");
            // fall through to code defaults
        }
        return new PromptSettings
        {
            Model = DefaultModel(),
            SystemInstruction = DefaultSystem(),
            IncludeKnowledgeBase = false,
            Effort = GenerationTuningCatalog.DefaultEffort,
            MaxOutputTokens = GenerationTuningCatalog.DefaultMaxOutputTokens,
        };
    }

    private bool Persist()
    {
        string? temp = null;
        try
        {
            _paths.EnsureDirs();
            temp = $"{_paths.SettingsFile}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(_settings, JsonOpts));
            File.Move(temp, _paths.SettingsFile, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not persist prompt settings to {SettingsFile}.", _paths.SettingsFile);
            if (temp is not null)
            {
                try { File.Delete(temp); } catch { /* best effort */ }
            }
            return false;
        }
    }

    private static PromptSettings Clone(PromptSettings s) => new()
    {
        Model = s.Model,
        SystemInstruction = s.SystemInstruction,
        IncludeKnowledgeBase = s.IncludeKnowledgeBase,
        Effort = s.Effort,
        MaxOutputTokens = s.MaxOutputTokens,
    };
}

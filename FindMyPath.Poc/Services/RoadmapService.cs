using Anthropic;
using Anthropic.Models.Messages;
using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Sends one message to the Claude API and returns a parsed roadmap + token usage + cost.
/// Robust by design: every failure becomes a friendly RoadmapResult, never an unhandled exception.
/// </summary>
public class RoadmapService
{
    private readonly PromptSettingsService _settings;

    public RoadmapService(PromptSettingsService settings) => _settings = settings;

    public async Task<RoadmapResult> GenerateAsync(AssessmentAnswers answers, CancellationToken ct = default)
    {
        var settings = _settings.Current;
        var model = string.IsNullOrWhiteSpace(settings.Model) ? ModelCatalog.DefaultModel : settings.Model;
        var userMessage = AssessmentFormatter.ToUserMessage(answers, settings.ReferenceMaterial);
        var result = new RoadmapResult { Model = model };

        var apiKey = _settings.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            result.Success = false;
            result.ErrorMessage = "The API key is missing. Add it to the app settings file " +
                                  "(%APPDATA%\\FindMyPath\\settings.json) or set the ANTHROPIC_API_KEY environment variable.";
            return result;
        }

        try
        {
            var client = new AnthropicClient { ApiKey = apiKey };

            var response = await client.Messages.Create(new MessageCreateParams
            {
                Model = model,
                MaxTokens = 16000,
                System = settings.SystemInstruction,
                Messages = [new() { Role = Role.User, Content = userMessage }],
            });

            var text = string.Concat(response.Content
                .Select(b => b.Value)
                .OfType<TextBlock>()
                .Select(t => t.Text));

            result.RawText = text;
            result.Usage = ExtractUsage(response.Usage);
            result.CostUsd = ModelCatalog.ComputeCost(model,
                result.Usage.InputTokens, result.Usage.OutputTokens,
                result.Usage.CacheReadTokens, result.Usage.CacheWriteTokens);
            result.Roadmap = RoadmapParser.TryParse(text);
            result.Success = true;
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = FriendlyError(ex);
            return result;
        }
    }

    private static TokenUsage ExtractUsage(Usage usage) => new()
    {
        InputTokens = usage.InputTokens,
        OutputTokens = usage.OutputTokens,
        CacheReadTokens = usage.CacheReadInputTokens ?? 0,
        CacheWriteTokens = usage.CacheCreationInputTokens ?? 0,
    };

    private static string FriendlyError(Exception ex)
    {
        var msg = (ex.Message ?? "").ToLowerInvariant();
        if (msg.Contains("401") || msg.Contains("authentication") || msg.Contains("api key") || msg.Contains("x-api-key") || msg.Contains("unauthorized"))
            return "The API key appears to be missing or invalid. Check %APPDATA%\\FindMyPath\\settings.json.";
        if (msg.Contains("429") || msg.Contains("rate limit") || msg.Contains("overload"))
            return "The AI service is busy right now. Please wait a moment and try again.";
        if (msg.Contains("timeout") || msg.Contains("timed out") || msg.Contains("canceled"))
            return "The request took too long. Please try again.";
        return "The AI service could not be reached. Please check the connection and try again.";
    }
}

using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Anthropic;
using Anthropic.Helpers;
using Anthropic.Models.Messages;
using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Sends one message to the Claude API and returns a parsed roadmap + token usage + cost.
/// Robust by design: every failure becomes a friendly RoadmapResult, never an unhandled exception.
/// Knowledge-base files are attached to the request as native document/image content blocks.
/// </summary>
public class RoadmapService
{
    private readonly PromptSettingsService _settings;
    private readonly KnowledgeBaseService _kb;
    private readonly ILogger<RoadmapService>? _logger;

    // Keep the assembled request well under the Claude API's 32 MB request cap (base64 inflates ~33%).
    private const long AttachmentBase64Budget = 28L * 1024 * 1024;

    public RoadmapService(PromptSettingsService settings, KnowledgeBaseService kb,
        ILogger<RoadmapService>? logger = null)
    {
        _settings = settings;
        _kb = kb;
        _logger = logger;
    }

    public async Task<RoadmapResult> GenerateAsync(AssessmentAnswers answers, CancellationToken ct = default,
        bool? includeKnowledgeBase = null)
    {
        var req = BuildRequest(answers, includeKnowledgeBase);
        var result = req.NewResult();

        var apiKey = _settings.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            result.Success = false;
            result.ErrorMessage = MissingKeyMessage;
            return result;
        }

        try
        {
            var client = new AnthropicClient { ApiKey = apiKey };

            var response = await client.Messages.Create(CreateParameters(req), ct);

            var text = string.Concat(response.Content
                .Select(b => b.Value)
                .OfType<TextBlock>()
                .Select(t => t.Text));

            result.RawText = text;
            result.Usage = ExtractUsage(response.Usage);
            result.CostUsd = ModelCatalog.ComputeCost(req.Model,
                result.Usage.InputTokens, result.Usage.OutputTokens,
                result.Usage.CacheReadTokens, result.Usage.CacheWriteTokens);
            result.Roadmap = RoadmapParser.TryParse(text);
            result.Success = !string.IsNullOrWhiteSpace(text);
            if (!result.Success) result.ErrorMessage = "The AI returned an empty response. Please try again.";
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Roadmap generation failed for model {Model}.", req.Model);
            result.Success = false;
            result.ErrorMessage = FriendlyError(ex);
            return result;
        }
    }

    /// <summary>
    /// Streaming variant: surfaces text as it arrives via <paramref name="onDelta"/> for the live demo.
    /// Same result shape as GenerateAsync. On any failure it returns a Failed result; the caller can
    /// fall back to the non-streaming path.
    /// </summary>
    public async Task<RoadmapResult> GenerateStreamingAsync(AssessmentAnswers answers, Action<string> onDelta,
        CancellationToken ct = default, bool? includeKnowledgeBase = null)
    {
        var req = BuildRequest(answers, includeKnowledgeBase);
        var result = req.NewResult();

        var apiKey = _settings.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            result.Success = false;
            result.ErrorMessage = MissingKeyMessage;
            return result;
        }

        try
        {
            var client = new AnthropicClient { ApiKey = apiKey };
            var sb = new StringBuilder();
            long inTok = 0, outTok = 0, cacheRead = 0, cacheWrite = 0;

            var parameters = CreateParameters(req);

            await foreach (var ev in client.Messages.CreateStreaming(parameters, ct).WithCancellation(ct))
            {
                if (ev.TryPickContentBlockDelta(out var cbd) && cbd.Delta.TryPickText(out var td))
                {
                    sb.Append(td.Text);
                    onDelta(td.Text);
                }
                else if (ev.TryPickStart(out var start))
                {
                    var u = start.Message.Usage;
                    inTok = u.InputTokens;
                    cacheRead = u.CacheReadInputTokens ?? 0;
                    cacheWrite = u.CacheCreationInputTokens ?? 0;
                }
                else if (ev.TryPickDelta(out var md) && md.Usage is { } du)
                {
                    outTok = du.OutputTokens;
                }
            }

            var text = sb.ToString();
            result.RawText = text;
            result.Usage = new TokenUsage
            {
                InputTokens = inTok,
                OutputTokens = outTok,
                CacheReadTokens = cacheRead,
                CacheWriteTokens = cacheWrite,
            };
            result.CostUsd = ModelCatalog.ComputeCost(req.Model, inTok, outTok, cacheRead, cacheWrite);
            result.Roadmap = RoadmapParser.TryParse(text);
            result.Success = !string.IsNullOrWhiteSpace(text);
            if (!result.Success) result.ErrorMessage = "The AI returned an empty response. Please try again.";
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Streaming roadmap generation failed for model {Model}.", req.Model);
            result.Success = false;
            result.ErrorMessage = FriendlyError(ex);
            return result;
        }
    }

    // --- request assembly --------------------------------------------------

    private const string MissingKeyMessage =
        "The API key is missing. Add it to appsettings.json (Anthropic:ApiKey) " +
        "or set the ANTHROPIC_API_KEY environment variable.";

    /// <summary>The parts of a generation request, shared by the streaming and non-streaming paths.</summary>
    private sealed class RequestBuild
    {
        public required string Model { get; init; }
        public required string System { get; init; }         // includes the hidden JSON-output contract
        public required string EditableSystem { get; init; } // the visible-only instruction, for the snapshot
        public required string Effort { get; init; }
        public required int MaxOutputTokens { get; init; }
        public required bool IncludeKnowledgeBase { get; init; }
        public required List<ContentBlockParam> Content { get; init; }
        public required string UserMessage { get; init; }
        public required List<AttachmentInfo> Attachments { get; init; }

        public RoadmapResult NewResult() => new()
        {
            Model = Model,
            Effort = Effort,
            MaxOutputTokens = MaxOutputTokens,
            IncludeKnowledgeBase = IncludeKnowledgeBase,
            QuestionnaireVersion = AssessmentAnswers.QuestionnaireVersion,
            GeneratedAtUtc = DateTime.UtcNow.ToString("o"),
            SystemInstruction = EditableSystem,
            UserMessage = UserMessage,
            Attachments = Attachments,
        };
    }

    private RequestBuild BuildRequest(AssessmentAnswers answers, bool? includeKnowledgeBase)
    {
        var settings = _settings.Current;
        var model = string.IsNullOrWhiteSpace(settings.Model) ? ModelCatalog.DefaultModel : settings.Model;
        var effort = GenerationTuningCatalog.NormalizeEffort(settings.Effort);
        var maxOutputTokens = GenerationTuningCatalog.NormalizeMaxOutputTokens(settings.MaxOutputTokens);
        var useKnowledgeBase = includeKnowledgeBase ?? settings.IncludeKnowledgeBase;

        var editable = (settings.SystemInstruction ?? "").TrimEnd();
        // Structured outputs enforce the JSON shape; the appended contract controls useful content/length.
        var system = $"{editable}\n\n{DefaultPrompt.OutputFormatInstruction}";

        var userMessage = AssessmentFormatter.ToUserMessage(answers);

        var content = new List<ContentBlockParam> { new TextBlockParam(userMessage) };
        var attachments = new List<AttachmentInfo>();
        if (useKnowledgeBase) AppendKnowledgeBase(content, attachments);

        return new RequestBuild
        {
            Model = model,
            System = system,
            EditableSystem = editable,
            Effort = effort,
            MaxOutputTokens = maxOutputTokens,
            IncludeKnowledgeBase = useKnowledgeBase,
            Content = content,
            UserMessage = userMessage,
            Attachments = attachments,
        };
    }

    private static MessageCreateParams CreateParameters(RequestBuild req)
    {
        Effort? configuredEffort = null;
        if (ModelCatalog.Find(req.Model).SupportsEffort &&
            Enum.TryParse<Effort>(req.Effort, ignoreCase: true, out var parsedEffort))
        {
            configuredEffort = parsedEffort;
        }

        var output = new OutputConfig
        {
            Format = StructuredOutput.CreateJsonFormat<RoadmapDto>(),
            Effort = configuredEffort,
        };

        return new MessageCreateParams
        {
            Model = req.Model,
            MaxTokens = req.MaxOutputTokens,
            System = req.System,
            OutputConfig = output,
            Messages = [new() { Role = Role.User, Content = req.Content }],
        };
    }

    /// <summary>
    /// Turns each knowledge-base file into the right content block: text/Word → a plain-text document,
    /// PDFs → a PDF document, images → an image block. Anything else (or oversized) is recorded as
    /// "not sent". A short lead-in text block introduces the material to the model.
    /// </summary>
    private void AppendKnowledgeBase(List<ContentBlockParam> content, List<AttachmentInfo> manifest)
    {
        var files = _kb.List().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
        if (files.Count == 0) return;

        var sent = new List<ContentBlockParam>();
        long base64Used = 0;

        foreach (var f in files)
        {
            var info = new AttachmentInfo { FileName = f.Name, Bytes = f.SizeBytes };
            var bytes = _kb.ReadAllBytes(f.Name);

            if (bytes is null || bytes.Length == 0)
            {
                info.Kind = "not sent";
                info.Note = bytes is null ? "could not be read" : "empty file";
                manifest.Add(info);
                continue;
            }

            ContentBlockParam block;
            string kind;
            long wireSize;
            var ext = f.Extension;

            if (IsImageExt(ext, out var mediaType))
            {
                kind = "image";
                var b64 = Convert.ToBase64String(bytes);
                wireSize = b64.Length;
                block = new ImageBlockParam(new Base64ImageSource { Data = b64, MediaType = mediaType });
            }
            else if (ext == ".pdf")
            {
                kind = "PDF";
                var b64 = Convert.ToBase64String(bytes);
                wireSize = b64.Length;
                block = new DocumentBlockParam(new Base64PdfSource(b64)) { Title = f.Name };
            }
            else if (ext == ".docx" && TryExtractDocxText(bytes, out var docText))
            {
                kind = "Word document";
                wireSize = Encoding.UTF8.GetByteCount(docText);
                block = new DocumentBlockParam(new PlainTextSource(docText)) { Title = f.Name };
            }
            else if (IsTextExt(ext) || TryReadUtf8Text(bytes, out _))
            {
                kind = "text";
                var text = TryReadUtf8Text(bytes, out var t) ? t : Encoding.UTF8.GetString(bytes);
                wireSize = Encoding.UTF8.GetByteCount(text);
                block = new DocumentBlockParam(new PlainTextSource(text)) { Title = f.Name };
            }
            else
            {
                info.Kind = "not sent";
                info.Note = "unsupported file type — the AI accepts text, Word, PDF, and image files";
                manifest.Add(info);
                continue;
            }

            if (base64Used + wireSize > AttachmentBase64Budget)
            {
                info.Kind = "not sent";
                info.Note = "skipped to keep the request within the AI's 32 MB limit";
                manifest.Add(info);
                continue;
            }

            base64Used += wireSize;
            info.Kind = kind;
            info.Sent = true;
            manifest.Add(info);
            sent.Add(block);
        }

        if (sent.Count > 0)
        {
            content.Add(new TextBlockParam(
                "The following reference material was provided by HealthNet. Treat it as authoritative guidance."));
            content.AddRange(sent);
        }
    }

    private static bool IsImageExt(string ext, out MediaType mediaType)
    {
        switch (ext)
        {
            case ".jpg":
            case ".jpeg": mediaType = MediaType.ImageJpeg; return true;
            case ".png": mediaType = MediaType.ImagePng; return true;
            case ".gif": mediaType = MediaType.ImageGif; return true;
            case ".webp": mediaType = MediaType.ImageWebP; return true;
            default: mediaType = MediaType.ImagePng; return false;
        }
    }

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".markdown", ".text", ".csv", ".tsv", ".json", ".xml", ".html", ".htm",
        ".yaml", ".yml", ".log", ".rtf", ".ini", ".cfg", ".conf", ".sql", ".cs", ".js", ".ts",
        ".py", ".java", ".css", ".sh", ".bat", ".ps1"
    };

    private static bool IsTextExt(string ext) => TextExtensions.Contains(ext);

    /// <summary>True when the bytes decode as valid UTF-8 text and contain no NUL (a binary marker).</summary>
    private static bool TryReadUtf8Text(byte[] bytes, out string text)
    {
        try
        {
            var decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var s = decoder.GetString(bytes);
            if (s.IndexOf('\0') >= 0) { text = ""; return false; }
            text = s;
            return true;
        }
        catch
        {
            text = "";
            return false;
        }
    }

    /// <summary>Best-effort text extraction from a .docx (a zip of XML) using only the BCL.</summary>
    private static bool TryExtractDocxText(byte[] bytes, out string text)
    {
        text = "";
        try
        {
            using var ms = new MemoryStream(bytes);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            var entry = zip.GetEntry("word/document.xml");
            if (entry is null) return false;

            using var reader = new StreamReader(entry.Open());
            var xml = reader.ReadToEnd();

            xml = xml.Replace("<w:tab/>", "\t");
            xml = Regex.Replace(xml, "</w:p>", "\n");            // paragraph → newline
            var stripped = Regex.Replace(xml, "<[^>]+>", "");     // drop all tags
            var decoded = WebUtility.HtmlDecode(stripped);
            decoded = Regex.Replace(decoded, "[ \t]+\n", "\n");
            decoded = Regex.Replace(decoded, "\n{3,}", "\n\n").Trim();

            text = decoded;
            return text.Length > 0;
        }
        catch
        {
            return false;
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
            return "The API key appears to be missing or invalid. Check appsettings.json (Anthropic:ApiKey) or the ANTHROPIC_API_KEY environment variable.";
        if (msg.Contains("429") || msg.Contains("rate limit") || msg.Contains("overload"))
            return "The AI service is busy right now. Please wait a moment and try again.";
        if (msg.Contains("413") || msg.Contains("request too large") || msg.Contains("too large"))
            return "The request is too large — remove some knowledge-base files (or use smaller ones) and try again.";
        if (msg.Contains("timeout") || msg.Contains("timed out") || msg.Contains("canceled"))
            return "The request took too long. Please try again.";
        return "The AI service could not be reached. Please check the connection and try again.";
    }
}

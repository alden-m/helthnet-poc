namespace FindMyPath.Poc.Models;

/// <summary>A file stored in the app-data knowledge-base folder, listed in Settings.</summary>
public class KbFile
{
    public string Name { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    public string Extension => Path.GetExtension(Name).ToLowerInvariant();

    /// <summary>Human-readable size, e.g. "12.3 KB", "1.1 MB".</summary>
    public string SizeDisplay =>
        SizeBytes >= 1024 * 1024 ? $"{SizeBytes / (1024.0 * 1024.0):0.0} MB"
        : SizeBytes >= 1024 ? $"{SizeBytes / 1024.0:0.0} KB"
        : $"{SizeBytes} B";
}

/// <summary>
/// How one knowledge-base file was (or wasn't) attached to a generation request — surfaced in the
/// "what was sent" panel and the history snapshot so the demo is transparent about what the AI saw.
/// </summary>
public class AttachmentInfo
{
    public string FileName { get; set; } = "";
    public long Bytes { get; set; }

    /// <summary>Friendly kind: "text", "PDF", "image", "Word document", or "not sent".</summary>
    public string Kind { get; set; } = "";

    /// <summary>True when the file was included in the API request.</summary>
    public bool Sent { get; set; }

    /// <summary>For files that couldn't be sent, a short reason.</summary>
    public string? Note { get; set; }

    public string SizeDisplay =>
        Bytes >= 1024 * 1024 ? $"{Bytes / (1024.0 * 1024.0):0.0} MB"
        : Bytes >= 1024 ? $"{Bytes / 1024.0:0.0} KB"
        : $"{Bytes} B";
}

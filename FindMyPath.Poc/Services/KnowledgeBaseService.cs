using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Manages the knowledge-base folder under app-data: the files a user uploads in Settings that are sent
/// to the AI as reference material with every generation. All file operations are best-effort and never
/// throw to the UI. Filenames are sanitized and every path is confined to the knowledge-base directory.
/// </summary>
public class KnowledgeBaseService
{
    /// <summary>Per-file upload cap. Kept well under the Claude API's 32 MB total-request limit so the
    /// base64-inflated request (plus the questionnaire) stays within bounds even with a few files.</summary>
    public const long MaxFileBytes = 10 * 1024 * 1024;

    private readonly AppPaths _paths;

    public KnowledgeBaseService(AppPaths paths) => _paths = paths;

    /// <summary>All knowledge-base files, newest first.</summary>
    public List<KbFile> List()
    {
        var list = new List<KbFile>();
        try
        {
            _paths.EnsureDirs();
            foreach (var path in Directory.GetFiles(_paths.KnowledgeBaseDir))
            {
                try
                {
                    var info = new FileInfo(path);
                    list.Add(new KbFile
                    {
                        Name = info.Name,
                        SizeBytes = info.Length,
                        LastModifiedUtc = info.LastWriteTimeUtc,
                    });
                }
                catch { /* skip unreadable entries */ }
            }
        }
        catch { /* return whatever we have */ }
        return list.OrderByDescending(f => f.LastModifiedUtc).ToList();
    }

    /// <summary>Reads a file's bytes, or null if it can't be read.</summary>
    public byte[]? ReadAllBytes(string name)
    {
        try
        {
            var path = ResolveInsideKb(name);
            if (path is not null && File.Exists(path)) return File.ReadAllBytes(path);
        }
        catch { /* ignore */ }
        return null;
    }

    /// <summary>
    /// Saves an uploaded stream under the knowledge-base folder. Returns the stored file name, or throws
    /// <see cref="InvalidOperationException"/> with a friendly message the caller can surface if the file
    /// is too large or can't be saved.
    /// </summary>
    public async Task<string> SaveAsync(string uploadName, Stream content, CancellationToken ct = default)
    {
        _paths.EnsureDirs();

        var safeName = MakeUniqueName(SanitizeFileName(uploadName));
        var dest = Path.Combine(_paths.KnowledgeBaseDir, safeName);

        // Copy through a byte cap so an oversized upload can't fill the disk.
        try
        {
            await using var outStream = File.Create(dest);
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await content.ReadAsync(buffer, ct)) > 0)
            {
                total += read;
                if (total > MaxFileBytes)
                {
                    outStream.Close();
                    TryDelete(dest);
                    throw new InvalidOperationException(
                        $"\"{uploadName}\" is larger than the {MaxFileBytes / (1024 * 1024)} MB limit.");
                }
                await outStream.WriteAsync(buffer.AsMemory(0, read), ct);
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception)
        {
            TryDelete(dest);
            throw new InvalidOperationException($"Could not save \"{uploadName}\".");
        }

        return safeName;
    }

    public bool Delete(string name)
    {
        var path = ResolveInsideKb(name);
        if (path is null) return false;
        return TryDelete(path);
    }

    public int DeleteAll()
    {
        var count = 0;
        foreach (var f in List())
            if (Delete(f.Name)) count++;
        return count;
    }

    // --- helpers -----------------------------------------------------------

    /// <summary>Resolves a candidate file name to an absolute path and verifies it stays inside the KB dir.</summary>
    private string? ResolveInsideKb(string name)
    {
        try
        {
            var candidate = Path.GetFileName(name); // strip any directory components
            if (string.IsNullOrWhiteSpace(candidate)) return null;
            var full = Path.GetFullPath(Path.Combine(_paths.KnowledgeBaseDir, candidate));
            var root = Path.GetFullPath(_paths.KnowledgeBaseDir) + Path.DirectorySeparatorChar;
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
        }
        catch { return null; }
    }

    private static string SanitizeFileName(string name)
    {
        var justName = Path.GetFileName(name);
        if (string.IsNullOrWhiteSpace(justName)) justName = "file";
        foreach (var c in Path.GetInvalidFileNameChars())
            justName = justName.Replace(c, '_');
        justName = justName.Trim().Trim('.');
        return string.IsNullOrWhiteSpace(justName) ? "file" : justName;
    }

    /// <summary>Appends " (2)", " (3)", … before the extension if a file with that name already exists.</summary>
    private string MakeUniqueName(string name)
    {
        var dir = _paths.KnowledgeBaseDir;
        if (!File.Exists(Path.Combine(dir, name))) return name;

        var stem = Path.GetFileNameWithoutExtension(name);
        var ext = Path.GetExtension(name);
        for (var i = 2; i < 10000; i++)
        {
            var candidate = $"{stem} ({i}){ext}";
            if (!File.Exists(Path.Combine(dir, candidate))) return candidate;
        }
        return $"{stem} ({Guid.NewGuid():N}){ext}";
    }

    private static bool TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) { File.Delete(path); return true; }
        }
        catch { /* ignore */ }
        return false;
    }
}

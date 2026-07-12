using System.Text;
using FindMyPath.Poc.Services;
using FindMyPath.Poc.Tests.TestInfrastructure;

namespace FindMyPath.Poc.Tests.Services;

public class KnowledgeBaseServiceTests
{
    [Fact]
    public async Task SaveStripsDirectoryComponentsAndKeepsTheFileInsideKnowledgeBase()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        var service = new KnowledgeBaseService(paths);
        var payload = Encoding.UTF8.GetBytes("authoritative guidance");

        var storedName = await service.SaveAsync(
            "../outside/guide.md",
            new MemoryStream(payload, writable: false));

        Assert.Equal("guide.md", storedName);
        Assert.False(File.Exists(Path.Combine(paths.DataDir, "guide.md")));
        Assert.Equal(payload, service.ReadAllBytes("guide.md"));
        Assert.True(File.Exists(Path.Combine(paths.KnowledgeBaseDir, "guide.md")));
    }

    [Fact]
    public async Task SaveUsesSafeFallbackAndUniqueNamesWithoutOverwriting()
    {
        using var env = new TempAppEnvironment();
        var service = new KnowledgeBaseService(env.CreatePaths());

        var fallback = await service.SaveAsync(" ", StreamOf("fallback"));
        var first = await service.SaveAsync("guide.md", StreamOf("first"));
        var second = await service.SaveAsync("guide.md", StreamOf("second"));

        Assert.Equal("file", fallback);
        Assert.Equal("guide.md", first);
        Assert.Equal("guide (2).md", second);
        Assert.Equal("first", Encoding.UTF8.GetString(service.ReadAllBytes(first)!));
        Assert.Equal("second", Encoding.UTF8.GetString(service.ReadAllBytes(second)!));
        Assert.Equal(3, service.List().Count);
    }

    [Fact]
    public void ReadAndDeleteCannotEscapeTheKnowledgeBaseDirectory()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        paths.EnsureDirs();
        var outsidePath = Path.Combine(paths.DataDir, "secret.txt");
        File.WriteAllText(outsidePath, "secret");
        var service = new KnowledgeBaseService(paths);

        Assert.Null(service.ReadAllBytes("../secret.txt"));
        Assert.False(service.Delete("../secret.txt"));
        Assert.True(File.Exists(outsidePath));
    }

    [Fact]
    public async Task SaveAcceptsAFileAtTheExactSizeLimit()
    {
        using var env = new TempAppEnvironment();
        var service = new KnowledgeBaseService(env.CreatePaths());
        var bytes = new byte[checked((int)KnowledgeBaseService.MaxFileBytes)];
        bytes[0] = 1;
        bytes[^1] = 2;

        var name = await service.SaveAsync("at-limit.bin", new MemoryStream(bytes, writable: false));

        var loaded = service.ReadAllBytes(name);
        Assert.NotNull(loaded);
        Assert.Equal(KnowledgeBaseService.MaxFileBytes, loaded.LongLength);
        Assert.Equal(1, loaded[0]);
        Assert.Equal(2, loaded[^1]);
    }

    [Fact]
    public async Task SaveRejectsAnOversizedFileAndRemovesThePartialFile()
    {
        using var env = new TempAppEnvironment();
        var paths = env.CreatePaths();
        var service = new KnowledgeBaseService(paths);
        var bytes = new byte[checked((int)KnowledgeBaseService.MaxFileBytes + 1)];

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync("too-big.bin", new MemoryStream(bytes, writable: false)));

        Assert.Contains("larger than", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(service.List(), f => f.Name == "too-big.bin");
        Assert.False(File.Exists(Path.Combine(paths.KnowledgeBaseDir, "too-big.bin")));
    }

    [Fact]
    public async Task DeleteAndDeleteAllReportWhatTheyActuallyRemoved()
    {
        using var env = new TempAppEnvironment();
        var service = new KnowledgeBaseService(env.CreatePaths());
        var first = await service.SaveAsync("one.txt", StreamOf("1"));
        await service.SaveAsync("two.txt", StreamOf("2"));
        await service.SaveAsync("three.txt", StreamOf("3"));

        Assert.True(service.Delete(first));
        Assert.False(service.Delete(first));
        Assert.Equal(2, service.DeleteAll());
        Assert.Empty(service.List());
    }

    private static MemoryStream StreamOf(string value) =>
        new(Encoding.UTF8.GetBytes(value), writable: false);
}

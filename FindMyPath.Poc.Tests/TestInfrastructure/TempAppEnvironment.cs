using FindMyPath.Poc.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace FindMyPath.Poc.Tests.TestInfrastructure;

internal sealed class TempAppEnvironment : IWebHostEnvironment, IDisposable
{
    public TempAppEnvironment()
    {
        ContentRootPath = Path.Combine(Path.GetTempPath(), "FindMyPath.Poc.Tests", Guid.NewGuid().ToString("N"));
        WebRootPath = Path.Combine(ContentRootPath, "wwwroot");
        Directory.CreateDirectory(ContentRootPath);
    }

    public string ApplicationName { get; set; } = "FindMyPath.Poc.Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; }
    public string EnvironmentName { get; set; } = "Testing";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; }

    public AppPaths CreatePaths() => new(this);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(ContentRootPath))
            {
                Directory.Delete(ContentRootPath, recursive: true);
            }
        }
        catch
        {
            // A failed cleanup should not hide the behavior under test.
        }
    }
}

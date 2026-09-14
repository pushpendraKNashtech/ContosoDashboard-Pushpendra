using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Services;

public class LocalFileStorageServiceTests
{
    [Fact]
    public async Task SavesAndReadsFilesUnderConfiguredRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "contoso-document-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DocumentStorage:Root"] = "uploads" }).Build();
            var environment = new TestEnvironment(root);
            var storage = new LocalFileStorageService(configuration, environment);
            await using var input = new MemoryStream("hello"u8.ToArray());
            var path = await storage.SaveAsync(input, 4, null, ".txt");
            await using (var output = await storage.OpenReadAsync(path))
            using (var reader = new StreamReader(output!))
            {
                Assert.Equal("hello", await reader.ReadToEndAsync());
            }
            await storage.DeleteAsync(path);
            Assert.Null(await storage.OpenReadAsync(path));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void RejectsPathTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "contoso-document-tests", Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DocumentStorage:Root"] = "uploads" }).Build();
        var storage = new LocalFileStorageService(configuration, new TestEnvironment(root));
        Assert.Throws<InvalidOperationException>(() => storage.OpenReadAsync("../../outside.txt").GetAwaiter().GetResult());
        Directory.Delete(root, true);
    }

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ContosoDashboard.Tests";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

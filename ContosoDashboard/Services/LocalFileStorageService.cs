namespace ContosoDashboard.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;

    public LocalFileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration["DocumentStorage:Root"] ?? "AppData/uploads";
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(userId.ToString(), projectId?.ToString() ?? "personal");
        var relative = Path.Combine(folder, $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
        var fullPath = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var output = File.Create(fullPath);
        await content.CopyToAsync(output, cancellationToken);
        return relative.Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(relativePath);
        Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(relativePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private string Resolve(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid document path.");
        return fullPath;
    }
}

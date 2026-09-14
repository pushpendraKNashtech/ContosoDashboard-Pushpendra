using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Services;

public class DocumentUploadTests
{
    [Fact]
    public async Task UploadPersistsMetadataAndAuditForAuthenticatedUser()
    {
        await using var context = CreateContext();
        var storage = new FakeStorage();
        var service = CreateService(context, storage);
        await using var content = new MemoryStream("document"u8.ToArray());

        var result = await service.UploadAsync(content, "notes.txt", "text/plain", content.Length,
            new DocumentUploadRequest("Notes", "Test document", "Personal Files", "training", null, null), 4);

        Assert.True(result.Success);
        Assert.NotNull(result.Document);
        Assert.Equal("Notes", result.Document!.Title);
        Assert.Equal(1, await context.Documents.CountAsync());
        Assert.Contains(context.DocumentActivities, activity => activity.Action == "Upload");
        Assert.Contains(storage.Paths, path => path.Contains("4/personal", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UploadRejectsUnsupportedTypeBeforeStorage()
    {
        await using var context = CreateContext();
        var storage = new FakeStorage();
        var service = CreateService(context, storage);
        await using var content = new MemoryStream("bad"u8.ToArray());

        var result = await service.UploadAsync(content, "payload.exe", "application/octet-stream", content.Length,
            new DocumentUploadRequest("Payload", null, "Other", null, null, null), 4);

        Assert.False(result.Success);
        Assert.Empty(storage.Paths);
        Assert.Empty(context.Documents);
    }

    [Fact]
    public async Task UploadRejectsUnauthorizedProject()
    {
        await using var context = CreateContext();
        var storage = new FakeStorage();
        var service = CreateService(context, storage);
        await using var content = new MemoryStream("document"u8.ToArray());

        var result = await service.UploadAsync(content, "notes.txt", "text/plain", content.Length,
            new DocumentUploadRequest("Notes", null, "Project Documents", null, 1, null), 1);

        Assert.False(result.Success);
        Assert.Empty(storage.Paths);
        Assert.Empty(context.Documents);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options);
    }

    private static DocumentService CreateService(ApplicationDbContext context, FakeStorage storage)
    {
        return new DocumentService(context, storage, new SafeScanner(), new NoopNotifications(), NullLogger<DocumentService>.Instance);
    }

    private sealed class FakeStorage : IFileStorageService
    {
        public List<string> Paths { get; } = [];
        private readonly Dictionary<string, byte[]> _files = [];
        public async Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default)
        {
            var path = $"{userId}/{projectId?.ToString() ?? "personal"}/{Guid.NewGuid():N}{extension}";
            using var memory = new MemoryStream();
            await content.CopyToAsync(memory, cancellationToken);
            _files[path] = memory.ToArray();
            Paths.Add(path);
            return path;
        }
        public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(_files.TryGetValue(relativePath, out var bytes) ? new MemoryStream(bytes) : null);
        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) { _files.Remove(relativePath); return Task.CompletedTask; }
    }

    private sealed class SafeScanner : IMalwareScanner
    {
        public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class NoopNotifications : INotificationService
    {
        public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification);
        public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>());
        public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0);
        public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false);
    }
}

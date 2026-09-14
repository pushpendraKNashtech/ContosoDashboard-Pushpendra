using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Services;

public class DocumentLifecycleTests
{
    [Fact]
    public async Task OwnerCanUpdateMetadata()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { UserId = 4, Email = "owner@test", DisplayName = "Owner", Role = UserRole.Employee });
        context.Documents.Add(NewDocument());
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var updated = await service.UpdateMetadataAsync(1, new DocumentUploadRequest("Updated", "Description", "Reports", "tag", null, null), 4);

        Assert.True(updated);
        Assert.Equal("Updated", (await context.Documents.FindAsync(1))!.Title);
    }

    [Fact]
    public async Task OwnerDeleteRemovesFileAndRetainsAuditEvent()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { UserId = 4, Email = "owner@test", DisplayName = "Owner", Role = UserRole.Employee });
        context.Documents.Add(NewDocument());
        await context.SaveChangesAsync();
        var storage = new FakeStorage();
        var service = new DocumentService(context, storage, new SafeScanner(), new NoopNotifications(), NullLogger<DocumentService>.Instance);

        var deleted = await service.DeleteAsync(1, 4);

        Assert.True(deleted);
        Assert.Empty(context.Documents);
        Assert.Contains(context.DocumentActivities, activity => activity.Action == "Delete");
        Assert.Contains(storage.Deleted, path => path == "4/personal/file.txt");
    }

    [Fact]
    public async Task OwnerCanReplaceSupportedFile()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { UserId = 4, Email = "owner@test", DisplayName = "Owner", Role = UserRole.Employee });
        context.Documents.Add(NewDocument());
        await context.SaveChangesAsync();
        var storage = new FakeStorage();
        var service = new DocumentService(context, storage, new SafeScanner(), new NoopNotifications(), NullLogger<DocumentService>.Instance);
        await using var content = new MemoryStream("replacement"u8.ToArray());

        var replaced = await service.ReplaceAsync(1, content, "replacement.txt", "text/plain", content.Length, 4);

        Assert.True(replaced);
        Assert.Equal("replacement.txt", (await context.Documents.FindAsync(1))!.OriginalFileName);
        Assert.Contains(context.DocumentActivities, activity => activity.Action == "Replace");
    }

    private static Document NewDocument() => new() { DocumentId = 1, Title = "Original", Category = "Other", OriginalFileName = "file.txt", FilePath = "4/personal/file.txt", FileType = "text/plain", FileSizeBytes = 10, UploadedByUserId = 4 };
    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static DocumentService CreateService(ApplicationDbContext context) => new(context, new FakeStorage(), new SafeScanner(), new NoopNotifications(), NullLogger<DocumentService>.Instance);
    private sealed class FakeStorage : IFileStorageService { public List<string> Deleted { get; } = []; public Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default) => Task.FromResult("new.txt"); public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(new MemoryStream([1])); public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) { Deleted.Add(relativePath); return Task.CompletedTask; } }
    private sealed class SafeScanner : IMalwareScanner { public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(true); }
    private sealed class NoopNotifications : INotificationService { public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification); public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>()); public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0); public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false); }
}

using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Services;

public class DocumentActivityTests
{
    [Fact]
    public async Task LifecycleActionsAreAttributableAndDoNotExposeFilenameOrContent()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.AddRange(new User { UserId = 2, Email = "recipient@test", DisplayName = "Recipient", Role = UserRole.Employee }, new User { UserId = 4, Email = "owner@test", DisplayName = "Owner", Role = UserRole.Employee });
        await context.SaveChangesAsync();
        var service = new DocumentService(context, new Storage(), new SafeScanner(), new NoopNotifications(), NullLogger<DocumentService>.Instance);
        await using var uploadContent = new MemoryStream("confidential file body"u8.ToArray());

        var upload = await service.UploadAsync(uploadContent, "secret-plan.txt", "text/plain", uploadContent.Length, new DocumentUploadRequest("Plan", null, "Other", null, null, null), 4);
        Assert.True(upload.Success);
        Assert.True(await service.UpdateMetadataAsync(upload.Document!.DocumentId, new DocumentUploadRequest("Updated", null, "Reports", null, null, null), 4));
        await using var replacement = new MemoryStream("replacement body"u8.ToArray());
        Assert.True(await service.ReplaceAsync(upload.Document.DocumentId, replacement, "replacement.txt", "text/plain", replacement.Length, 4));
        Assert.True(await service.ShareAsync(upload.Document.DocumentId, 2, 4));
        Assert.True(await service.RevokeShareAsync(upload.Document.DocumentId, 2, 4));
        Assert.True(await service.DeleteAsync(upload.Document.DocumentId, 4));

        var activities = await context.DocumentActivities.ToListAsync();
        Assert.All(activities, activity => Assert.Equal(4, activity.UserId));
        Assert.Contains(activities, activity => activity.Action == "Upload");
        Assert.Contains(activities, activity => activity.Action == "MetadataUpdate");
        Assert.Contains(activities, activity => activity.Action == "Replace");
        Assert.Contains(activities, activity => activity.Action == "Share");
        Assert.Contains(activities, activity => activity.Action == "RevokeShare");
        Assert.Contains(activities, activity => activity.Action == "Delete");
        Assert.All(activities, activity => { Assert.DoesNotContain("secret-plan.txt", activity.Details ?? string.Empty); Assert.DoesNotContain("confidential file body", activity.Details ?? string.Empty); });
    }

    private sealed class Storage : IFileStorageService { public Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default) => Task.FromResult($"4/personal/{Guid.NewGuid():N}{extension}"); public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null); public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class SafeScanner : IMalwareScanner { public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(true); }
    private sealed class NoopNotifications : INotificationService { public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification); public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>()); public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0); public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false); }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Services;

public class DocumentActivityTests
{
    [Fact]
    public async Task UploadAndDeleteCreateAttributableActivityRecords()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.Add(new User { UserId = 4, Email = "owner@test", DisplayName = "Owner", Role = UserRole.Employee });
        await context.SaveChangesAsync();
        var storage = new Storage();
        var service = new DocumentService(context, storage, new SafeScanner(), new Notifications(), NullLogger<DocumentService>.Instance);
        await using var content = new MemoryStream("audit"u8.ToArray());

        var upload = await service.UploadAsync(content, "audit.txt", "text/plain", content.Length, new DocumentUploadRequest("Audit", null, "Other", null, null, null), 4);
        Assert.True(upload.Success);
        Assert.True(await service.DeleteAsync(upload.Document!.DocumentId, 4));

        var actions = context.DocumentActivities.Select(a => a.Action).ToList();
        Assert.Contains("Upload", actions);
        Assert.Contains("Delete", actions);
        Assert.All(context.DocumentActivities, activity => Assert.DoesNotContain("audit.txt", activity.Details ?? string.Empty));
    }

    private sealed class Storage : IFileStorageService { private readonly Dictionary<string, byte[]> files = []; public async Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default) { using var memory = new MemoryStream(); await content.CopyToAsync(memory, cancellationToken); var path = $"{userId}/personal/file{extension}"; files[path] = memory.ToArray(); return path; } public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(files.TryGetValue(relativePath, out var bytes) ? new MemoryStream(bytes) : null); public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) { files.Remove(relativePath); return Task.CompletedTask; } }
    private sealed class SafeScanner : IMalwareScanner { public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(true); }
    private sealed class Notifications : INotificationService { public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification); public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>()); public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0); public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false); }
}

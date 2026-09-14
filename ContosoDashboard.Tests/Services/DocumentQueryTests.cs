using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Services;

public class DocumentQueryTests
{
    [Fact]
    public async Task SearchSortsByTitleAndAppliesSearchTerm()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { UserId = 4, Email = "owner@test", DisplayName = "Owner", Role = UserRole.Employee });
        context.Documents.AddRange(
            NewDocument(1, "Zeta report", 4),
            NewDocument(2, "Alpha notes", 4));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var results = await service.SearchAsync(4, new DocumentQuery("notes", null, null, null, null, "title", false));

        Assert.Single(results);
        Assert.Equal("Alpha notes", results[0].Title);
    }

    private static Document NewDocument(int id, string title, int owner) => new() { DocumentId = id, Title = title, Category = "Other", OriginalFileName = $"{id}.txt", FilePath = $"{owner}/personal/{id}.txt", FileType = "text/plain", FileSizeBytes = 10, UploadedByUserId = owner };
    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static DocumentService CreateService(ApplicationDbContext context) => new(context, new NullStorage(), new SafeScanner(), new NoopNotifications(), NullLogger<DocumentService>.Instance);
    private sealed class NullStorage : IFileStorageService { public Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty); public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null); public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class SafeScanner : IMalwareScanner { public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(true); }
    private sealed class NoopNotifications : INotificationService { public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification); public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>()); public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0); public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false); }
}

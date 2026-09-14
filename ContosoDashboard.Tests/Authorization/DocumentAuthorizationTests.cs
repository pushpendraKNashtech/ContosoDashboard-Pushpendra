using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Authorization;

public class DocumentAuthorizationTests
{
    [Fact]
    public async Task SearchDoesNotReturnAnotherUsersPersonalDocument()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User { UserId = 2, Email = "manager@test", DisplayName = "Manager", Role = UserRole.ProjectManager },
            new User { UserId = 4, Email = "employee@test", DisplayName = "Employee", Role = UserRole.Employee });
        context.Documents.Add(new Document { DocumentId = 1, Title = "Private", Category = "Personal Files", OriginalFileName = "private.txt", FilePath = "4/personal/private.txt", FileType = "text/plain", FileSizeBytes = 10, UploadedByUserId = 4 });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var results = await service.SearchAsync(2, new DocumentQuery(null, null, null, null, null));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchReturnsActiveDirectShareToRecipient()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User { UserId = 2, Email = "recipient@test", DisplayName = "Recipient", Role = UserRole.Employee },
            new User { UserId = 4, Email = "owner@test", DisplayName = "Owner", Role = UserRole.Employee });
        context.Documents.Add(new Document { DocumentId = 1, Title = "Shared", Category = "Other", OriginalFileName = "shared.txt", FilePath = "4/personal/shared.txt", FileType = "text/plain", FileSizeBytes = 10, UploadedByUserId = 4, Shares = [new DocumentShare { SharedWithUserId = 2, SharedByUserId = 4, IsActive = true }] });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var results = await service.SearchAsync(2, new DocumentQuery(null, null, null, null, null));

        Assert.Single(results);
        Assert.Equal("Shared", results[0].Title);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options);
    }

    private static DocumentService CreateService(ApplicationDbContext context) => new(context, new NullStorage(), new SafeScanner(), new NoopNotifications(), NullLogger<DocumentService>.Instance);

    private sealed class NullStorage : IFileStorageService
    {
        public Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class SafeScanner : IMalwareScanner { public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(true); }
    private sealed class NoopNotifications : INotificationService
    {
        public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification);
        public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>());
        public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0);
        public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false);
    }
}

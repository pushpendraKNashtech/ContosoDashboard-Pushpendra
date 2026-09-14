using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Services;

public class DocumentSharingTests
{
    [Fact]
    public async Task OwnerCanShareAndRecipientIsNotified()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.AddRange(
            new User { UserId = 2, Email = "recipient@test", DisplayName = "Recipient", Role = UserRole.Employee },
            new User { UserId = 4, Email = "owner@test", DisplayName = "Owner", Role = UserRole.Employee });
        context.Documents.Add(new Document { DocumentId = 1, Title = "Shared", Category = "Other", OriginalFileName = "shared.txt", FilePath = "4/personal/shared.txt", FileType = "text/plain", FileSizeBytes = 10, UploadedByUserId = 4 });
        await context.SaveChangesAsync();
        var notifications = new CapturingNotifications();
        var service = new DocumentService(context, new NullStorage(), new SafeScanner(), notifications, NullLogger<DocumentService>.Instance);

        var shared = await service.ShareAsync(1, 2, 4);

        Assert.True(shared);
        Assert.Single(context.DocumentShares);
        Assert.Single(notifications.Created);
        Assert.Equal(NotificationType.DocumentShared, notifications.Created[0].Type);
    }

    [Fact]
    public async Task DuplicateDirectShareIsIdempotentAndOwnerCanRevokeIt()
    {
        await using var context = CreateContext();
        var notifications = new CapturingNotifications();
        var service = CreateService(context, notifications);

        Assert.True(await service.ShareAsync(1, 2, 4));
        Assert.True(await service.ShareAsync(1, 2, 4));
        Assert.Single(context.DocumentShares);
        Assert.Single(notifications.Created);
        Assert.True(await service.RevokeShareAsync(1, 2, 4));
        Assert.False((await context.DocumentShares.SingleAsync()).IsActive);
    }

    [Fact]
    public async Task OwnerCanShareWithDepartmentAndDepartmentRecipientCanFindDocument()
    {
        await using var context = CreateContext();
        var notifications = new CapturingNotifications();
        var service = CreateService(context, notifications);

        Assert.True(await service.ShareWithDepartmentAsync(1, "Engineering", 4));
        Assert.Single(context.DocumentShares);
        Assert.Single(notifications.Created);
        Assert.Single(await service.SearchAsync(2, new DocumentQuery(null, null, null, null, null, SharedOnly: true)));
    }

    [Fact]
    public async Task UnauthorizedUserCannotShareOrRevoke()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new CapturingNotifications());

        Assert.False(await service.ShareAsync(1, 2, 3));
        Assert.False(await service.RevokeShareAsync(1, 2, 3));
        Assert.Empty(context.DocumentShares);
    }

    private static ApplicationDbContext CreateContext()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.AddRange(
            new User { UserId = 2, Email = "recipient@test", DisplayName = "Recipient", Department = "Engineering", Role = UserRole.Employee },
            new User { UserId = 3, Email = "other@test", DisplayName = "Other", Department = "Sales", Role = UserRole.Employee },
            new User { UserId = 4, Email = "owner@test", DisplayName = "Owner", Department = "Engineering", Role = UserRole.Employee });
        context.Documents.Add(new Document { DocumentId = 1, Title = "Shared", Category = "Other", OriginalFileName = "shared.txt", FilePath = "4/personal/shared.txt", FileType = "text/plain", FileSizeBytes = 10, UploadedByUserId = 4 });
        context.SaveChanges();
        return context;
    }

    private static DocumentService CreateService(ApplicationDbContext context, CapturingNotifications notifications) => new(context, new NullStorage(), new SafeScanner(), notifications, NullLogger<DocumentService>.Instance);

    private sealed class NullStorage : IFileStorageService { public Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty); public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null); public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class SafeScanner : IMalwareScanner { public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(true); }
    private sealed class CapturingNotifications : INotificationService { public List<Notification> Created { get; } = []; public Task<Notification> CreateNotificationAsync(Notification notification) { Created.Add(notification); return Task.FromResult(notification); } public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>()); public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0); public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false); }
}

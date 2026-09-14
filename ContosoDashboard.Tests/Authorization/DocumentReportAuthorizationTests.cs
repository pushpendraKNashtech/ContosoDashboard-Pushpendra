using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Authorization;

public class DocumentReportAuthorizationTests
{
    [Fact]
    public async Task OnlyAdministratorCanReadReport()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.AddRange(
            new User { UserId = 1, Email = "admin@test", DisplayName = "Admin", Role = UserRole.Administrator },
            new User { UserId = 4, Email = "employee@test", DisplayName = "Employee", Role = UserRole.Employee });
        await context.SaveChangesAsync();
        var service = new DocumentService(context, new NullStorage(), new SafeScanner(), new NoopNotifications(), NullLogger<DocumentService>.Instance);

        Assert.Null(await service.GetReportAsync(4));
        Assert.NotNull(await service.GetReportAsync(1));
    }

    private sealed class NullStorage : IFileStorageService { public Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty); public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null); public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class SafeScanner : IMalwareScanner { public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(true); }
    private sealed class NoopNotifications : INotificationService { public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification); public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>()); public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0); public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false); }
}

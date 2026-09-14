using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Integration;

public class TaskDocumentIntegrationTests
{
    [Fact]
    public async Task AuthorizedTaskUserGetsOnlyDocumentsAttachedToThatTask()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.Add(new User { UserId = 4, Email = "employee@test", DisplayName = "Employee", Role = UserRole.Employee });
        context.Tasks.Add(new TaskItem { TaskId = 1, Title = "Task", AssignedUserId = 4, CreatedByUserId = 4 });
        context.Documents.AddRange(
            new Document { DocumentId = 1, TaskId = 1, Title = "Attached", Category = "Other", OriginalFileName = "a.txt", FilePath = "4/personal/a.txt", FileType = "text/plain", FileSizeBytes = 1, UploadedByUserId = 4 },
            new Document { DocumentId = 2, Title = "Unrelated", Category = "Other", OriginalFileName = "b.txt", FilePath = "4/personal/b.txt", FileType = "text/plain", FileSizeBytes = 1, UploadedByUserId = 4 });
        await context.SaveChangesAsync();
        var service = new TaskService(context, new NoopNotifications());

        var documents = await service.GetTaskDocumentsAsync(1, 4);

        Assert.Single(documents);
        Assert.Equal("Attached", documents[0].Title);
    }

    private sealed class NoopNotifications : INotificationService
    {
        public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification);
        public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>());
        public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0);
        public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false);
    }
}

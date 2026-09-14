using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Integration;

public class TaskDocumentIntegrationTests
{
    [Fact]
    public async Task AuthorizedTaskUserGetsOnlyDocumentsAttachedToThatTask()
    {
        await using var context = CreateContext();
        context.Documents.AddRange(NewDocument(1, 1), NewDocument(2, null));
        await context.SaveChangesAsync();

        var documents = await new TaskService(context, new NoopNotifications()).GetTaskDocumentsAsync(1, 4);

        Assert.Single(documents);
        Assert.Equal("Attached", documents[0].Title);
    }

    [Fact]
    public async Task UploadRejectsTaskThatDoesNotBelongToSelectedProject()
    {
        await using var context = CreateContext();
        context.Projects.Add(new Project { ProjectId = 2, Name = "Other", ProjectManagerId = 2, Status = ProjectStatus.Active, ProjectMembers = [new ProjectMember { UserId = 4, Role = "Member" }] });
        context.Tasks.Add(new TaskItem { TaskId = 2, Title = "Other task", ProjectId = 2, AssignedUserId = 4, CreatedByUserId = 2 });
        await context.SaveChangesAsync();
        var service = new DocumentService(context, new Storage(), new SafeScanner(), new NoopNotifications(), NullLogger<DocumentService>.Instance);
        await using var content = new MemoryStream("task"u8.ToArray());

        var result = await service.UploadAsync(content, "task.txt", "text/plain", content.Length, new DocumentUploadRequest("Task file", null, "Other", null, 1, 2), 4);

        Assert.False(result.Success);
        Assert.Empty(context.Documents);
    }

    private static ApplicationDbContext CreateContext()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.AddRange(new User { UserId = 2, Email = "manager@test", DisplayName = "Manager", Role = UserRole.ProjectManager }, new User { UserId = 4, Email = "member@test", DisplayName = "Member", Role = UserRole.Employee });
        context.Projects.Add(new Project { ProjectId = 1, Name = "Project", ProjectManagerId = 2, Status = ProjectStatus.Active, ProjectMembers = [new ProjectMember { UserId = 4, Role = "Member" }] });
        context.Tasks.Add(new TaskItem { TaskId = 1, Title = "Task", ProjectId = 1, AssignedUserId = 4, CreatedByUserId = 2 });
        context.SaveChanges();
        return context;
    }

    private static Document NewDocument(int id, int? taskId) => new() { DocumentId = id, TaskId = taskId, Title = taskId.HasValue ? "Attached" : "Unrelated", Category = "Other", OriginalFileName = $"{id}.txt", FilePath = $"4/personal/{id}.txt", FileType = "text/plain", FileSizeBytes = 1, UploadedByUserId = 4 };
    private sealed class Storage : IFileStorageService { public Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default) => Task.FromResult("4/personal/new.txt"); public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null); public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class SafeScanner : IMalwareScanner { public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(true); }
    private sealed class NoopNotifications : INotificationService { public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification); public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>()); public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0); public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false); }
}

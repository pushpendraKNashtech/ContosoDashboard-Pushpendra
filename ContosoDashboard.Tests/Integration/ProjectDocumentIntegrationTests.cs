using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Integration;

public class ProjectDocumentIntegrationTests
{
    [Fact]
    public async Task ProjectMemberGetsProjectDocumentsAndNonMemberDoesNot()
    {
        await using var context = CreateContext();
        context.Documents.Add(NewDocument(1, 4));
        await context.SaveChangesAsync();
        var service = new ProjectService(context);

        Assert.Single(await service.GetProjectDocumentsAsync(1, 4));
        Assert.Empty(await service.GetProjectDocumentsAsync(1, 5));
    }

    [Fact]
    public async Task ProjectManagerCanUploadAndMembersAreNotified()
    {
        await using var context = CreateContext();
        var notifications = new CapturingNotifications();
        var service = new DocumentService(context, new Storage(), new SafeScanner(), notifications, NullLogger<DocumentService>.Instance);
        await using var content = new MemoryStream("project"u8.ToArray());

        var result = await service.UploadAsync(content, "project.txt", "text/plain", content.Length, new DocumentUploadRequest("Project file", null, "Project Documents", null, 1, null), 2);

        Assert.True(result.Success);
        Assert.Single(notifications.Created);
        Assert.Equal(4, notifications.Created[0].UserId);
        Assert.Equal(NotificationType.DocumentAddedToProject, notifications.Created[0].Type);
    }

    [Fact]
    public async Task DashboardReturnsDocumentCountAndFiveMostRecentUploads()
    {
        await using var context = CreateContext();
        context.Documents.AddRange(Enumerable.Range(1, 6).Select(id => NewDocument(id, 4, DateTime.UtcNow.AddMinutes(-id))));
        await context.SaveChangesAsync();
        var dashboard = new DashboardService(context);

        var summary = await dashboard.GetDashboardSummaryAsync(4);
        var recent = await dashboard.GetRecentDocumentsAsync(4);

        Assert.Equal(6, summary.DocumentCount);
        Assert.Equal(5, recent.Count);
        Assert.Equal("Document 1", recent[0].Title);
    }

    private static ApplicationDbContext CreateContext()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.AddRange(
            new User { UserId = 2, Email = "manager@test", DisplayName = "Manager", Role = UserRole.ProjectManager },
            new User { UserId = 4, Email = "member@test", DisplayName = "Member", Role = UserRole.Employee },
            new User { UserId = 5, Email = "other@test", DisplayName = "Other", Role = UserRole.Employee });
        context.Projects.Add(new Project { ProjectId = 1, Name = "Project", ProjectManagerId = 2, Status = ProjectStatus.Active, ProjectMembers = [new ProjectMember { UserId = 4, Role = "Member" }] });
        context.SaveChanges();
        return context;
    }

    private static Document NewDocument(int id, int uploaderId, DateTime? uploadedDate = null) => new() { DocumentId = id, ProjectId = 1, Title = $"Document {id}", Category = "Project Documents", OriginalFileName = $"{id}.txt", FilePath = $"4/1/{id}.txt", FileType = "text/plain", FileSizeBytes = 1, UploadedByUserId = uploaderId, UploadedDate = uploadedDate ?? DateTime.UtcNow };
    private sealed class Storage : IFileStorageService { public Task<string> SaveAsync(Stream content, int userId, int? projectId, string extension, CancellationToken cancellationToken = default) => Task.FromResult("2/1/new.txt"); public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null); public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class SafeScanner : IMalwareScanner { public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(true); }
    private sealed class CapturingNotifications : INotificationService { public List<Notification> Created { get; } = []; public Task<Notification> CreateNotificationAsync(Notification notification) { Created.Add(notification); return Task.FromResult(notification); } public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>()); public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0); public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false); }
}

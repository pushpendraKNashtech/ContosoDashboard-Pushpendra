using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Integration;

public class ProjectDocumentIntegrationTests
{
    [Fact]
    public async Task ProjectMemberGetsProjectDocumentsAndNonMemberDoesNot()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Users.AddRange(
            new User { UserId = 2, Email = "manager@test", DisplayName = "Manager", Role = UserRole.ProjectManager },
            new User { UserId = 4, Email = "member@test", DisplayName = "Member", Role = UserRole.Employee },
            new User { UserId = 5, Email = "other@test", DisplayName = "Other", Role = UserRole.Employee });
        context.Projects.Add(new Project { ProjectId = 1, Name = "Project", ProjectManagerId = 2, ProjectMembers = [new ProjectMember { UserId = 4, Role = "Member" }] });
        context.Documents.Add(new Document { DocumentId = 1, ProjectId = 1, Title = "Project file", Category = "Project Documents", OriginalFileName = "project.txt", FilePath = "4/1/project.txt", FileType = "text/plain", FileSizeBytes = 1, UploadedByUserId = 4 });
        await context.SaveChangesAsync();
        var service = new ProjectService(context);

        Assert.Single(await service.GetProjectDocumentsAsync(1, 4));
        Assert.Empty(await service.GetProjectDocumentsAsync(1, 5));
    }
}

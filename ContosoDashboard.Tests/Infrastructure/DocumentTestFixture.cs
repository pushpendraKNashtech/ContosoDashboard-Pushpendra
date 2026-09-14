using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;

namespace ContosoDashboard.Tests.Infrastructure;

public sealed class DocumentTestFixture
{
    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"documents-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
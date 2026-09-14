using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ContosoDashboard.Models;
using ContosoDashboard.Pages;
using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Integration;

public class DocumentAccessEndpointTests
{
    [Fact]
    public async Task AuthorizedUserReceivesFileResult()
    {
        var service = new StubDocuments { File = new DocumentFile(new MemoryStream([1, 2]), "text/plain", "file.txt") };
        var model = CreateModel(service, 4);

        var result = await model.OnGetAsync(1);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("text/plain", file.ContentType);
        Assert.Equal("file.txt", file.FileDownloadName);
    }

    [Fact]
    public async Task MissingAuthorizationReturnsNotFound()
    {
        var model = CreateModel(new StubDocuments(), null);

        var result = await model.OnGetAsync(1);

        Assert.IsType<NotFoundResult>(result);
    }

    private static DocumentDownloadModel CreateModel(StubDocuments service, int? userId)
    {
        var model = new DocumentDownloadModel(service) { PageContext = new() };
        var identity = userId.HasValue ? new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())], "test") : new ClaimsIdentity();
        model.PageContext.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return model;
    }

    private sealed class StubDocuments : IDocumentService
    {
        public DocumentFile? File { get; set; }
        public Task<DocumentFile?> OpenAuthorizedAsync(int documentId, int userId, CancellationToken cancellationToken = default) => Task.FromResult(File);
        public Task<List<Document>> SearchAsync(int userId, DocumentQuery query) => Task.FromResult(new List<Document>());
        public Task<List<Document>> GetRecentAsync(int userId, int count = 5) => Task.FromResult(new List<Document>());
        public Task<DocumentUploadResult> UploadAsync(Stream content, string originalFileName, string contentType, long size, DocumentUploadRequest request, int userId, CancellationToken cancellationToken = default) => Task.FromResult(new DocumentUploadResult(false, null, null));
        public Task<Document?> GetAuthorizedAsync(int documentId, int userId) => Task.FromResult<Document?>(null);
        public Task<bool> UpdateMetadataAsync(int documentId, DocumentUploadRequest request, int userId) => Task.FromResult(false);
        public Task<bool> ReplaceAsync(int documentId, Stream content, string originalFileName, string contentType, long size, int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> DeleteAsync(int documentId, int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ShareAsync(int documentId, int recipientUserId, int userId) => Task.FromResult(false);
        public Task<bool> RevokeShareAsync(int documentId, int recipientUserId, int userId) => Task.FromResult(false);
        public Task<DocumentReport?> GetReportAsync(int userId) => Task.FromResult<DocumentReport?>(null);
    }
}

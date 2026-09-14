using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<List<Document>> SearchAsync(int userId, DocumentQuery query);
    Task<List<Document>> GetRecentAsync(int userId, int count = 5);
    Task<DocumentUploadResult> UploadAsync(Stream content, string originalFileName, string contentType, long size, DocumentUploadRequest request, int userId, CancellationToken cancellationToken = default);
    Task<Document?> GetAuthorizedAsync(int documentId, int userId);
    Task<DocumentFile?> OpenAuthorizedAsync(int documentId, int userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMetadataAsync(int documentId, DocumentUploadRequest request, int userId);
    Task<bool> ReplaceAsync(int documentId, Stream content, string originalFileName, string contentType, long size, int userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int documentId, int userId, CancellationToken cancellationToken = default);
    Task<bool> ShareAsync(int documentId, int recipientUserId, int userId);
    Task<bool> RevokeShareAsync(int documentId, int recipientUserId, int userId);
    Task<DocumentReport?> GetReportAsync(int userId);
}

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IMalwareScanner _scanner;
    private readonly INotificationService _notifications;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(ApplicationDbContext context, IFileStorageService storage, IMalwareScanner scanner, INotificationService notifications, ILogger<DocumentService> logger)
    { _context = context; _storage = storage; _scanner = scanner; _notifications = notifications; _logger = logger; }

    public async Task<List<Document>> SearchAsync(int userId, DocumentQuery query)
    {
        var documents = _context.Documents
            .Include(d => d.UploadedByUser).Include(d => d.Project)
            .Where(d => d.UploadedByUserId == userId ||
                (d.ProjectId.HasValue && (d.Project!.ProjectManagerId == userId || d.Project.ProjectMembers.Any(m => m.UserId == userId))) ||
                d.Shares.Any(s => s.IsActive && (s.SharedWithUserId == userId || s.SharedWithDepartment == _context.Users.Where(u => u.UserId == userId).Select(u => u.Department).FirstOrDefault())) ||
                _context.Users.Any(u => u.UserId == userId && u.Role == UserRole.Administrator));
            if (query.TaskId.HasValue) documents = documents.Where(d => d.TaskId == query.TaskId);
            if (query.SharedOnly) documents = documents.Where(d => d.Shares.Any(s => s.IsActive && (s.SharedWithUserId == userId || s.SharedWithDepartment == _context.Users.Where(u => u.UserId == userId).Select(u => u.Department).FirstOrDefault())));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            documents = documents.Where(d => d.Title.Contains(term) || (d.Description ?? "").Contains(term) || (d.Tags ?? "").Contains(term) || d.UploadedByUser.DisplayName.Contains(term) || (d.Project != null && d.Project.Name.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(query.Category)) documents = documents.Where(d => d.Category == query.Category);
        if (query.ProjectId.HasValue) documents = documents.Where(d => d.ProjectId == query.ProjectId);
        if (query.From.HasValue) documents = documents.Where(d => d.UploadedDate >= query.From.Value);
        if (query.To.HasValue) documents = documents.Where(d => d.UploadedDate < query.To.Value.Date.AddDays(1));
        documents = query.Sort.ToLowerInvariant() switch { "title" => query.Descending ? documents.OrderByDescending(d => d.Title) : documents.OrderBy(d => d.Title), "category" => query.Descending ? documents.OrderByDescending(d => d.Category) : documents.OrderBy(d => d.Category), "size" => query.Descending ? documents.OrderByDescending(d => d.FileSizeBytes) : documents.OrderBy(d => d.FileSizeBytes), _ => query.Descending ? documents.OrderByDescending(d => d.UploadedDate) : documents.OrderBy(d => d.UploadedDate) };
        return await documents.Take(500).ToListAsync();
    }

    public Task<List<Document>> GetRecentAsync(int userId, int count = 5) => _context.Documents.Include(d => d.Project).Where(d => d.UploadedByUserId == userId).OrderByDescending(d => d.UploadedDate).Take(count).ToListAsync();

    public async Task<DocumentUploadResult> UploadAsync(Stream content, string originalFileName, string contentType, long size, DocumentUploadRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        if (size <= 0 || size > DocumentRules.MaxFileSize) return new(false, "Files must be between 1 byte and 25 MB.", null);
        if (!DocumentRules.AllowedTypes.TryGetValue(extension, out var expectedType) || !string.Equals(expectedType, contentType, StringComparison.OrdinalIgnoreCase)) return new(false, "This file type is not supported.", null);
        if (string.IsNullOrWhiteSpace(request.Title) || !DocumentRules.Categories.Contains(request.Category)) return new(false, "A title and approved category are required.", null);
        if (request.ProjectId.HasValue && !await CanAccessProjectAsync(request.ProjectId.Value, userId)) return new(false, "You are not authorized for this project.", null);
        if (request.TaskId.HasValue)
        {
            var task = await _context.Tasks.Include(t => t.Project).ThenInclude(p => p!.ProjectMembers).FirstOrDefaultAsync(t => t.TaskId == request.TaskId.Value, cancellationToken);
            if (task == null || !await CanAccessTaskAsync(task, userId)) return new(false, "You are not authorized for this task.", null);
            if (request.ProjectId.HasValue && task.ProjectId != request.ProjectId) return new(false, "The task and project do not match.", null);
        }
        if (content.CanSeek) content.Position = 0;
        if (!await _scanner.IsSafeAsync(content, cancellationToken)) return new(false, "The file did not pass security scanning.", null);
        if (content.CanSeek) content.Position = 0;
        string? path = null;
        try
        {
            path = await _storage.SaveAsync(content, userId, request.ProjectId, extension, cancellationToken);
            var document = new Document { Title = request.Title.Trim(), Description = request.Description?.Trim(), Category = request.Category, Tags = request.Tags?.Trim(), OriginalFileName = Path.GetFileName(originalFileName), FilePath = path, FileType = contentType, FileSizeBytes = size, UploadedByUserId = userId, ProjectId = request.ProjectId, TaskId = request.TaskId };
            _context.Documents.Add(document);
            _context.DocumentActivities.Add(new DocumentActivity { Document = document, UserId = userId, Action = "Upload", Details = "Document uploaded" });
            await _context.SaveChangesAsync(cancellationToken);
            return new(true, null, document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document upload failed for user {UserId}", userId);
            if (path != null) await _storage.DeleteAsync(path, cancellationToken);
            return new(false, "The document could not be saved.", null);
        }
    }

    public Task<Document?> GetAuthorizedAsync(int documentId, int userId) => GetAuthorizedQuery(userId).Include(d => d.Project).Include(d => d.UploadedByUser).FirstOrDefaultAsync(d => d.DocumentId == documentId);

    public async Task<DocumentFile?> OpenAuthorizedAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await GetAuthorizedAsync(documentId, userId);
        if (document == null) return null;
        var content = await _storage.OpenReadAsync(document.FilePath, cancellationToken);
        if (content == null) return null;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = document.FileType.StartsWith("image/") || document.FileType == "application/pdf" ? "Preview" : "Download" });
        await _context.SaveChangesAsync(cancellationToken);
        return new(content, document.FileType, document.OriginalFileName);
    }

    public async Task<bool> UpdateMetadataAsync(int documentId, DocumentUploadRequest request, int userId)
    {
        var document = await GetAuthorizedAsync(documentId, userId);
        if (document == null || (document.UploadedByUserId != userId && !await IsManagerAsync(document, userId))) return false;
        if (string.IsNullOrWhiteSpace(request.Title) || !DocumentRules.Categories.Contains(request.Category)) return false;
        document.Title = request.Title.Trim(); document.Description = request.Description?.Trim(); document.Category = request.Category; document.Tags = request.Tags?.Trim(); document.UpdatedDate = DateTime.UtcNow;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = "MetadataUpdate" });
        await _context.SaveChangesAsync(); return true;
    }

    public async Task<bool> DeleteAsync(int documentId, int userId, CancellationToken cancellationToken = default)
    {
        var document = await GetAuthorizedAsync(documentId, userId);
        if (document == null || (document.UploadedByUserId != userId && !await IsManagerAsync(document, userId))) return false;
        var oldPath = document.FilePath;
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
        try
        {
            await _storage.DeleteAsync(oldPath, cancellationToken);
            _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = "Delete" });
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document deletion cleanup failed for document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<bool> ReplaceAsync(int documentId, Stream content, string originalFileName, string contentType, long size, int userId, CancellationToken cancellationToken = default)
    {
        var document = await GetAuthorizedAsync(documentId, userId);
        if (document == null || document.UploadedByUserId != userId) return false;
        var extension = Path.GetExtension(originalFileName);
        if (size <= 0 || size > DocumentRules.MaxFileSize || !DocumentRules.AllowedTypes.TryGetValue(extension, out var expected) || !string.Equals(expected, contentType, StringComparison.OrdinalIgnoreCase)) return false;
        if (content.CanSeek) content.Position = 0;
        if (!await _scanner.IsSafeAsync(content, cancellationToken)) return false;
        if (content.CanSeek) content.Position = 0;
        var oldPath = document.FilePath;
        var newPath = await _storage.SaveAsync(content, userId, document.ProjectId, extension, cancellationToken);
        try
        {
            document.FilePath = newPath; document.OriginalFileName = Path.GetFileName(originalFileName); document.FileType = contentType; document.FileSizeBytes = size; document.UpdatedDate = DateTime.UtcNow;
            _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = "Replace" });
            await _context.SaveChangesAsync(cancellationToken);
            await _storage.DeleteAsync(oldPath, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document replacement failed for document {DocumentId}", documentId);
            await _storage.DeleteAsync(newPath, cancellationToken);
            return false;
        }
    }

    public async Task<bool> ShareAsync(int documentId, int recipientUserId, int userId)
    {
        var document = await GetAuthorizedAsync(documentId, userId);
        if (document == null || (document.UploadedByUserId != userId && !await IsManagerAsync(document, userId))) return false;
        if (!await _context.Users.AnyAsync(u => u.UserId == recipientUserId)) return false;
        if (await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.SharedWithUserId == recipientUserId && s.IsActive)) return true;
        _context.DocumentShares.Add(new DocumentShare { DocumentId = documentId, SharedWithUserId = recipientUserId, SharedByUserId = userId });
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = "Share" });
        await _context.SaveChangesAsync();
        await _notifications.CreateNotificationAsync(new Notification { UserId = recipientUserId, Title = "Document Shared", Message = $"A document was shared with you: {document.Title}", Type = NotificationType.DocumentShared, Priority = NotificationPriority.Informational });
        return true;
    }

    public async Task<bool> RevokeShareAsync(int documentId, int recipientUserId, int userId)
    {
        var document = await GetAuthorizedAsync(documentId, userId);
        if (document == null || (document.UploadedByUserId != userId && !await IsManagerAsync(document, userId))) return false;
        var share = await _context.DocumentShares.FirstOrDefaultAsync(s => s.DocumentId == documentId && s.SharedWithUserId == recipientUserId && s.IsActive);
        if (share == null) return false;
        share.IsActive = false;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = userId, Action = "RevokeShare" });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DocumentReport?> GetReportAsync(int userId)
    {
        if (!await _context.Users.AnyAsync(u => u.UserId == userId && u.Role == UserRole.Administrator)) return null;
        var byType = await _context.Documents.GroupBy(d => d.FileType).Select(g => new { Type = g.Key, Count = g.Count() }).ToListAsync();
        var byUploader = await _context.Documents.Include(d => d.UploadedByUser).GroupBy(d => d.UploadedByUser.DisplayName).Select(g => new { User = g.Key, Count = g.Count() }).ToListAsync();
        var byAction = await _context.DocumentActivities.GroupBy(a => a.Action).Select(g => new { Action = g.Key, Count = g.Count() }).ToListAsync();
        return new DocumentReport(await _context.Documents.CountAsync(), byType.Select(x => (x.Type, x.Count)).ToList(), byUploader.Select(x => (x.User, x.Count)).ToList(), byAction.Select(x => (x.Action, x.Count)).ToList());
    }

    private IQueryable<Document> GetAuthorizedQuery(int userId) => _context.Documents.Where(d => d.UploadedByUserId == userId || (d.ProjectId.HasValue && (d.Project!.ProjectManagerId == userId || d.Project.ProjectMembers.Any(m => m.UserId == userId))) || d.Shares.Any(s => s.IsActive && (s.SharedWithUserId == userId || s.SharedWithDepartment == _context.Users.Where(u => u.UserId == userId).Select(u => u.Department).FirstOrDefault())) || _context.Users.Any(u => u.UserId == userId && u.Role == UserRole.Administrator));
    private async Task<bool> CanAccessProjectAsync(int projectId, int userId) => await _context.Projects.AnyAsync(p => p.ProjectId == projectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId)));
    private Task<bool> CanAccessTaskAsync(TaskItem task, int userId) => Task.FromResult(task.AssignedUserId == userId || task.CreatedByUserId == userId || task.Project?.ProjectManagerId == userId || task.Project?.ProjectMembers.Any(m => m.UserId == userId) == true);
    private async Task<bool> IsManagerAsync(Document document, int userId) => await _context.Users.AnyAsync(u => u.UserId == userId && u.Role == UserRole.Administrator) || (document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId));
}

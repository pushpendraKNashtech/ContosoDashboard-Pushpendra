using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public static class DocumentRules
{
    public const long MaxFileSize = 25 * 1024 * 1024;
    public static readonly string[] Categories = ["Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other"];
    public static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf", [".doc"] = "application/msword", [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel", [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint", [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png"
    };
}

public sealed class DocumentUploadRequest
{
    public DocumentUploadRequest(string title, string? description, string category, string? tags, int? projectId, int? taskId)
    { Title = title; Description = description; Category = category; Tags = tags; ProjectId = projectId; TaskId = taskId; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; }
    public string? Tags { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
}
public sealed record DocumentUploadResult(bool Success, string? Error, Document? Document);
public sealed record DocumentFile(Stream Content, string ContentType, string FileName);
public sealed record DocumentQuery(string? Search, string? Category, int? ProjectId, DateTime? From, DateTime? To, string Sort = "date", bool Descending = true, int? TaskId = null, bool SharedOnly = false);
public sealed record DocumentShareRequest(int RecipientUserId);
public sealed record DocumentReport(int TotalDocuments, IReadOnlyList<(string Type, int Count)> ByType, IReadOnlyList<(string User, int Count)> ByUploader, IReadOnlyList<(string Action, int Count)> ByAction);

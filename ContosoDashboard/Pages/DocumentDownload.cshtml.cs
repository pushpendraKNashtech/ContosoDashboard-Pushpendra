using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using ContosoDashboard.Services;

namespace ContosoDashboard.Pages;

[Authorize]
public class DocumentDownloadModel : PageModel
{
    private readonly IDocumentService _documents;
    public DocumentDownloadModel(IDocumentService documents) => _documents = documents;

    public async Task<IActionResult> OnGetAsync(int documentId, bool download = false, CancellationToken cancellationToken = default)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId)) return NotFound();
        var file = await _documents.OpenAuthorizedAsync(documentId, userId, cancellationToken);
        if (file == null) return NotFound();
        return File(file.Content, file.ContentType, file.FileName);
    }
}

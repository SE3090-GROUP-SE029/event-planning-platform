using System.Security.Claims;
using Api.GuestManagement;
using Application.GuestManagement;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Planner-only endpoint for bulk guest list upload via CSV.
/// Route: POST /api/events/{eventId}/registration-form/upload
/// This is an ADDITIONAL entry point into the existing guest registration pipeline.
/// The public self-registration flow is not modified.
/// </summary>
[ApiController]
[Route("api/events/{eventId:guid}/registration-form")]
[ServiceFilter(typeof(PlannerAccessFilter))]
[ServiceFilter(typeof(RegistrationExceptionFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class GuestListUploadController(BulkGuestUploadService uploadService) : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private string PlannerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Accepts a CSV file (multipart/form-data) and creates Guest + RegistrationSubmission
    /// records with PENDING_AI status for each valid, non-duplicate guest row.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(Guid eventId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { code = "file_required", error = "A file is required." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { code = "file_too_large", error = $"File must not exceed {MaxFileSizeBytes / 1024 / 1024} MB." });

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        var validExtensions = new[] { ".csv", ".pdf", ".docx" };
        if (!validExtensions.Contains(extension))
        {
            return BadRequest(new { code = "invalid_file_type", error = "Only CSV, PDF, and DOCX files are supported." });
        }

        using var stream = file.OpenReadStream();
        var result = await uploadService.ProcessAsync(eventId, PlannerId, stream, extension, ct);
        return Ok(result);
    }
}

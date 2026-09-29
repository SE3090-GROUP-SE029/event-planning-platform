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
            return BadRequest(new { code = "file_required", error = "A CSV file is required." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { code = "file_too_large", error = $"File must not exceed {MaxFileSizeBytes / 1024 / 1024} MB." });

        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        if (!contentType.Contains("csv") && !contentType.Contains("text/plain") &&
            !contentType.Contains("octet-stream") && extension != ".csv")
        {
            return BadRequest(new { code = "invalid_file_type", error = "Only CSV files are supported. Expected a .csv file." });
        }

        string csvContent;
        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            csvContent = await reader.ReadToEndAsync(ct);
        }
        catch (Exception)
        {
            return BadRequest(new { code = "file_read_error", error = "Could not read the uploaded file." });
        }

        var result = await uploadService.ProcessAsync(eventId, PlannerId, csvContent, ct);
        return Ok(result);
    }
}

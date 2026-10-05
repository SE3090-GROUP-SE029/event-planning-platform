using System.Security.Claims;
using System.Net.Http.Headers;
using System.Diagnostics;
using Api.GuestManagement;
using Application.GuestManagement;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Planner-only endpoint for queueing public registration links for a guest list.
/// Route: POST /api/events/{eventId}/registration-form/upload
/// This is an ADDITIONAL entry point into the existing guest registration pipeline.
/// The public self-registration flow is not modified.
/// </summary>
[ApiController]
[Route("api/events/{eventId:guid}/registration-form")]
[ServiceFilter(typeof(PlannerAccessFilter))]
[ServiceFilter(typeof(RegistrationExceptionFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class GuestListUploadController(
    BulkGuestUploadService uploadService,
    ILogger<GuestListUploadController> logger) : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const string DocxMimeType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private string PlannerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// Accepts a CSV, PDF, or DOCX file and queues the public registration link for each
    /// valid, non-duplicate guest row. Registration records are created on form submission.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSizeBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> Upload(
        Guid eventId,
        [FromForm(Name = "file")] IFormFile? file,
        CancellationToken ct)
    {
        var requestStartedAt = HttpContext.Items.TryGetValue(
            GuestUploadTimingMiddleware.RequestStartedAtKey, out var startTimestamp) &&
            startTimestamp is long timestamp
                ? timestamp
                : Stopwatch.GetTimestamp();
        logger.LogInformation(
            "Guest list upload file received and model binding completed. EventId={EventId}, FileBytes={FileBytes}, ElapsedMs={ElapsedMs}",
            eventId,
            file?.Length ?? 0,
            Stopwatch.GetElapsedTime(requestStartedAt).TotalMilliseconds);
        logger.LogInformation(
            "Guest list upload user authenticated. EventId={EventId}, PlannerId={PlannerId}",
            eventId,
            PlannerId);

        if (file is null || file.Length == 0)
            return UploadValidationFailure(
                eventId,
                "INVALID_FILE",
                "A non-empty file is required.",
                [new GuestUploadRowError(0, "file", "A non-empty file is required.")]);

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        var mediaType = GetMediaType(file.ContentType);
        logger.LogInformation(
            "Guest list file received. EventId={EventId}, FileName={FileName}, FileBytes={FileBytes}, ContentType={ContentType}, Extension={Extension}",
            eventId,
            Path.GetFileName(file.FileName),
            file.Length,
            file.ContentType,
            extension);
        logger.LogInformation(
            "Guest list MIME type detected. EventId={EventId}, MediaType={MediaType}",
            eventId,
            mediaType);

        if (file.Length > MaxFileSizeBytes)
            return UploadValidationFailure(
                eventId,
                "INVALID_FILE",
                $"File must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.",
                [new GuestUploadRowError(0, "file", $"File must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.")]);

        logger.LogInformation(
            "Guest list file size validated. EventId={EventId}, FileBytes={FileBytes}",
            eventId,
            file.Length);

        if (!IsSupportedFile(extension, mediaType))
        {
            return UploadValidationFailure(
                eventId,
                "INVALID_FILE",
                "File extension and MIME type must match a supported CSV, PDF, or DOCX format.",
                [new GuestUploadRowError(0, "file", "Only CSV, PDF, and DOCX files with a supported MIME type are accepted.")]);
        }

        logger.LogInformation(
            "Guest list file validated. EventId={EventId}, Extension={Extension}, FileBytes={FileBytes}, MediaType={MediaType}",
            eventId,
            extension,
            file.Length,
            mediaType);
        logger.LogInformation(
            "Guest list parser selected. EventId={EventId}, Parser={Parser}",
            eventId,
            extension);

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await uploadService.ProcessAsync(eventId, PlannerId, stream, extension, ct);
            if (result.UploadedGuests == 0 &&
                result.InvalidRows == result.TotalRows &&
                result.Duplicates == 0)
            {
                return UploadValidationFailure(
                    eventId,
                    "INVALID_FILE",
                    "No valid guest rows were found in the uploaded file.",
                    result.Errors);
            }

            logger.LogInformation(
                "Guest list upload response prepared. EventId={EventId}, UploadedGuests={UploadedGuests}, QueuedEmails={QueuedEmails}, InvalidRows={InvalidRows}, DuplicateCount={DuplicateCount}, ElapsedMs={ElapsedMs}",
                eventId,
                result.UploadedGuests,
                result.QueuedEmails,
                result.InvalidRows,
                result.Duplicates,
                Stopwatch.GetElapsedTime(requestStartedAt).TotalMilliseconds);

            return Ok(new
            {
                success = true,
                uploadedGuests = result.UploadedGuests,
                queuedEmails = result.QueuedEmails,
                invalidRows = result.InvalidRows,
                duplicates = result.Duplicates,
                deliveryFailedRows = result.DeliveryFailedRows,
                totalRows = result.TotalRows,
                successfulRows = result.SuccessfulRows,
                failedRows = result.FailedRows,
                duplicateRows = result.DuplicateRows,
                alreadyRegisteredRows = result.AlreadyRegisteredRows,
                errors = result.Errors
            });
        }
        catch (RegistrationException exception)
        {
            logger.LogWarning(
                "Guest list upload validation failed. EventId={EventId}, ErrorCode={ErrorCode}, TraceId={TraceId}",
                eventId,
                exception.Code,
                HttpContext.TraceIdentifier);
            return UploadValidationFailure(
                eventId,
                exception.Code.ToUpperInvariant(),
                exception.Message,
                [new GuestUploadRowError(0, "file", exception.Message)],
                exception.StatusCode);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogWarning(
                "Guest list upload was cancelled. EventId={EventId}, TraceId={TraceId}",
                eventId,
                HttpContext.TraceIdentifier);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Guest list upload failed unexpectedly. EventId={EventId}, TraceId={TraceId}",
                eventId,
                HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                errorCode = "UPLOAD_FAILED",
                message = "The guest list could not be processed. Please retry; contact support with the trace ID if the problem continues.",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    private IActionResult UploadValidationFailure(
        Guid eventId,
        string errorCode,
        string message,
        IReadOnlyList<GuestUploadRowError> details,
        int statusCode = StatusCodes.Status400BadRequest)
    {
        logger.LogInformation(
            "Guest list upload response returned. EventId={EventId}, StatusCode={StatusCode}, ErrorCode={ErrorCode}, DetailCount={DetailCount}",
            eventId,
            statusCode,
            errorCode,
            details.Count);
        return StatusCode(statusCode, new
        {
            success = false,
            errorCode,
            message,
            details
        });
    }

    private static string GetMediaType(string? contentType)
        => MediaTypeHeaderValue.TryParse(contentType, out var parsed)
            ? parsed.MediaType?.ToLowerInvariant() ?? string.Empty
            : string.Empty;

    private static bool IsSupportedFile(string extension, string mediaType)
    {
        var supportedTypes = extension switch
        {
            ".csv" => new[] { "text/csv", "application/csv", "application/vnd.ms-excel", "text/plain" },
            ".pdf" => new[] { "application/pdf" },
            ".docx" => new[] { DocxMimeType, "application/zip" },
            _ => []
        };

        return supportedTypes.Length > 0 &&
               (supportedTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase) ||
                string.Equals(mediaType, "application/octet-stream", StringComparison.OrdinalIgnoreCase));
    }
}

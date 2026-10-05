using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.GuestManagement;

/// <summary>
/// Processes a planner-uploaded guest list and emails each valid guest the public
/// registration link. Guests enter the registration pipeline only after submission.
///
/// Public form submissions are handled by RegistrationService.
/// </summary>
public class BulkGuestUploadService(
    IGuestRegistrationRepository repository,
    GuestRegistrationOptions guestOptions,
    TimeProvider clock,
    ILogger<BulkGuestUploadService> logger)
{
    private const int MaxRows = 1000;

    /// <summary>
    /// Parses and validates the upload, persists guests and durable email jobs in one
    /// transaction, and returns without waiting for SMTP delivery.
    /// </summary>
    public async Task<BulkGuestUploadResult> ProcessAsync(
        Guid eventId,
        string plannerId,
        Stream fileStream,
        string extension,
        CancellationToken ct)
    {
        var processStarted = Stopwatch.GetTimestamp();
        var normalizedExtension = extension.ToLowerInvariant();
        var parseStarted = Stopwatch.GetTimestamp();
        logger.LogInformation(
            "Guest list parsing started. EventId={EventId}, Extension={Extension}",
            eventId,
            normalizedExtension);
        var (parsedRows, parseErrors) = normalizedExtension switch
        {
            ".pdf" => GuestDocumentParser.ParsePdf(fileStream, logger),
            ".docx" => GuestDocumentParser.ParseDocx(fileStream, logger),
            ".csv" => GuestCsvParser.Parse(new StreamReader(fileStream).ReadToEnd()),
            _ => throw new RegistrationException(400, "invalid_file_type",
                "Only CSV, PDF, and DOCX files are supported.")
        };
        var parseElapsed = Stopwatch.GetElapsedTime(parseStarted);
        logger.LogInformation(
            "Guest list file parsed and guest extraction completed. EventId={EventId}, ExtractedRows={ExtractedRows}, InvalidRows={InvalidRows}, ElapsedMs={ElapsedMs}",
            eventId,
            parsedRows.Count,
            parseErrors.Count,
            parseElapsed.TotalMilliseconds);
        var initialParseErrorCount = parseErrors.Count;
        if (initialParseErrorCount > 0)
        {
            logger.LogWarning(
                "Guest list parser reported file or row errors. EventId={EventId}, ErrorCount={ErrorCount}, FirstError={FirstError}",
                eventId,
                initialParseErrorCount,
                parseErrors[0].Message);
        }

        var duplicateStarted = Stopwatch.GetTimestamp();
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateRows = new List<GuestUploadRowError>();
        var candidateRows = new List<(GuestUploadRow Row, GuestDetails Details)>();

        foreach (var row in parsedRows)
        {
            GuestDetails details;
            try
            {
                details = RegistrationValidator.Validate(
                    new GuestDetails(row.FullName!, row.EmailAddress!, row.Organisation, row.PhoneNumber));
            }
            catch (RegistrationException ex)
            {
                parseErrors.Add(new GuestUploadRowError(row.RowNumber, DeriveField(ex.Code), ex.Message));
                continue;
            }

            if (!seenEmails.Add(details.EmailAddress.ToUpperInvariant()))
            {
                duplicateRows.Add(new GuestUploadRowError(row.RowNumber, "emailAddress",
                    $"Row {row.RowNumber}: email '{details.EmailAddress}' appears more than once in this file."));
                continue;
            }

            candidateRows.Add((row, details));
        }
        logger.LogInformation(
            "Guest list row validation completed. EventId={EventId}, GuestCount={GuestCount}, DuplicateCount={DuplicateCount}, InvalidRowCount={InvalidRowCount}, ElapsedMs={ElapsedMs}",
            eventId,
            candidateRows.Count,
            duplicateRows.Count,
            parseErrors.Count,
            Stopwatch.GetElapsedTime(duplicateStarted).TotalMilliseconds);

        var dbStarted = Stopwatch.GetTimestamp();
        var queueResult = await repository.WithEventLockAsync(eventId, async eventDetails =>
        {
            RegistrationAccess.RequireOwner(eventDetails, plannerId);

            var form = await repository.FindFormAsync(eventId, ct)
                ?? throw new RegistrationException(409, "form_required",
                    "The event must have a registration form before inviting guests.");
            if (form.Status != RegistrationFormStatus.PUBLISHED || form.PublicId is null)
                throw new RegistrationException(409, "form_not_published",
                    "The registration form must be published before inviting guests.");

            var normalizedEmails = candidateRows
                .Select(candidate => candidate.Details.EmailAddress.ToUpperInvariant())
                .ToArray();
            var registeredEmails = await repository.RegisteredEmailsAsync(eventId, normalizedEmails, ct);
            var existingGuests = await repository.FindGuestsByEmailsAsync(eventId, normalizedEmails, ct);
            var guestsByEmail = existingGuests.ToDictionary(guest => guest.NormalizedEmail, StringComparer.Ordinal);
            var now = clock.GetUtcNow();
            var newGuests = new List<Guest>();
            var emailJobs = new List<RegistrationLinkEmailJob>();
            var alreadyRegisteredCount = 0;
            var linkCreationStarted = Stopwatch.GetTimestamp();
            var registrationUrl = RegistrationUrl(form.PublicId);

            foreach (var (row, details) in candidateRows)
            {
                var normalizedEmail = details.EmailAddress.ToUpperInvariant();
                if (registeredEmails.Contains(normalizedEmail))
                {
                    alreadyRegisteredCount++;
                    continue;
                }

                if (!guestsByEmail.TryGetValue(normalizedEmail, out var guest))
                {
                    guest = new Guest
                    {
                        EventId = eventId,
                        FullName = details.FullName,
                        EmailAddress = details.EmailAddress,
                        NormalizedEmail = normalizedEmail,
                        Organisation = details.Organisation,
                        PhoneNumber = details.PhoneNumber,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    newGuests.Add(guest);
                    guestsByEmail.Add(normalizedEmail, guest);
                }
                else
                {
                    guest.FullName = details.FullName;
                    guest.EmailAddress = details.EmailAddress;
                    guest.Organisation = details.Organisation;
                    guest.PhoneNumber = details.PhoneNumber;
                    guest.UpdatedAt = now;
                }

                emailJobs.Add(new RegistrationLinkEmailJob
                {
                    EventId = eventId,
                    EmailAddress = details.EmailAddress,
                    FullName = details.FullName,
                    EventName = eventDetails.EventName,
                    RegistrationUrl = registrationUrl,
                    CreatedAt = now,
                    NextAttemptAt = now
                });
            }

            logger.LogInformation(
                "Guest registration links created. EventId={EventId}, LinkCount={LinkCount}, ElapsedMs={ElapsedMs}",
                eventId,
                emailJobs.Count,
                Stopwatch.GetElapsedTime(linkCreationStarted).TotalMilliseconds);
            logger.LogInformation(
                "Guest database insert started. EventId={EventId}, NewGuests={NewGuests}, UpdatedGuests={UpdatedGuests}, EmailJobs={EmailJobs}",
                eventId,
                newGuests.Count,
                emailJobs.Count - newGuests.Count,
                emailJobs.Count);
            repository.AddGuests(newGuests);
            repository.AddRegistrationLinkEmailJobs(emailJobs);
            return (
                UploadedGuests: emailJobs.Count,
                AlreadyRegistered: alreadyRegisteredCount,
                QueuedEmails: emailJobs.Count);
        }, ct);
        logger.LogInformation(
            "Guest database inserts and email queue committed. EventId={EventId}, UploadedGuests={UploadedGuests}, QueuedEmails={QueuedEmails}, ElapsedMs={ElapsedMs}",
            eventId,
            queueResult.UploadedGuests,
            queueResult.QueuedEmails,
            Stopwatch.GetElapsedTime(dbStarted).TotalMilliseconds);
        logger.LogInformation(
            "Guest duplicate detection completed. EventId={EventId}, InFileDuplicates={InFileDuplicates}, AlreadyRegistered={AlreadyRegistered}, TotalDuplicates={TotalDuplicates}",
            eventId,
            duplicateRows.Count,
            queueResult.AlreadyRegistered,
            duplicateRows.Count + queueResult.AlreadyRegistered);

        var allErrors = parseErrors
            .Concat(duplicateRows)
            .OrderBy(e => e.RowNumber)
            .ToList();

        var totalRows = parsedRows.Count + initialParseErrorCount;
        logger.LogInformation(
            "Guest list upload processing completed. EventId={EventId}, TotalElapsedMs={ElapsedMs}",
            eventId,
            Stopwatch.GetElapsedTime(processStarted).TotalMilliseconds);

        return new BulkGuestUploadResult(
            TotalRows: totalRows,
            SuccessfulRows: queueResult.QueuedEmails,
            FailedRows: parseErrors.Count,
            DuplicateRows: duplicateRows.Count,
            AlreadyRegisteredRows: queueResult.AlreadyRegistered,
            Errors: allErrors,
            UploadedGuests: queueResult.UploadedGuests,
            InvalidRows: parseErrors.Count,
            DeliveryFailedRows: 0,
            QueuedEmails: queueResult.QueuedEmails);
    }

    private string RegistrationUrl(string publicId)
        => new Uri(guestOptions.GetPublicWebBaseUri(),
            $"guest/register/{Uri.EscapeDataString(publicId)}").AbsoluteUri;

    private static string DeriveField(string errorCode) => errorCode switch
    {
        "invalid_email" => "emailAddress",
        "required_field" => "fullName",
        "invalid_field" => "field",
        _ => "field",
    };
}

/// <summary>
/// Static CSV parsing utilities for the guest list upload flow.
/// Public so unit tests can exercise CSV logic in isolation.
/// </summary>
public static class GuestCsvParser
{
    private const int MaxRows = 1000;
    private static readonly string[] RequiredHeaders = ["fullname", "emailaddress"];

    public static (List<GuestUploadRow> Rows, List<GuestUploadRowError> Errors) Parse(string csv)
    {
        var rows = new List<GuestUploadRow>();
        var errors = new List<GuestUploadRowError>();

        if (string.IsNullOrWhiteSpace(csv))
        {
            errors.Add(new GuestUploadRowError(0, "file", "The uploaded file is empty."));
            return (rows, errors);
        }

        using var reader = new StringReader(csv.ReplaceLineEndings("\n"));
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            errors.Add(new GuestUploadRowError(0, "file", "The CSV file has no header row."));
            return (rows, errors);
        }

        var headers = SplitLine(headerLine)
            .Select(h => h.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", ""))
            .ToArray();

        foreach (var required in RequiredHeaders)
        {
            if (!headers.Contains(required))
            {
                errors.Add(new GuestUploadRowError(0, "header",
                    $"Missing required column '{required}'. Expected columns: fullName, emailAddress, organisation (optional), phoneNumber (optional)."));
                return (rows, errors);
            }
        }

        int ColIndex(string name) => Array.FindIndex(headers, h => h == name);

        var fullNameIdx = ColIndex("fullname");
        var emailIdx = ColIndex("emailaddress");
        var orgIdx = ColIndex("organisation");
        var phoneIdx = ColIndex("phonenumber");

        var rowNumber = 1;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (rowNumber > MaxRows + 1)
            {
                errors.Add(new GuestUploadRowError(rowNumber, "file",
                    $"File exceeds the maximum of {MaxRows} guest rows. Only the first {MaxRows} rows were processed."));
                break;
            }

            var cells = SplitLine(line);

            string? Cell(int idx) =>
                idx >= 0 && idx < cells.Length
                    ? (cells[idx].Trim() is { Length: > 0 } v ? v : null)
                    : null;

            var fullName = Cell(fullNameIdx);
            var email = Cell(emailIdx);

            if (string.IsNullOrWhiteSpace(fullName))
            {
                errors.Add(new GuestUploadRowError(rowNumber, "fullName", $"Row {rowNumber}: fullName is required."));
                continue;
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add(new GuestUploadRowError(rowNumber, "emailAddress", $"Row {rowNumber}: emailAddress is required."));
                continue;
            }

            rows.Add(new GuestUploadRow(rowNumber, fullName, email, Cell(orgIdx), Cell(phoneIdx)));
        }

        return (rows, errors);
    }

    /// <summary>RFC 4180-compliant CSV field splitter (handles quoted fields with commas).</summary>
    public static string[] SplitLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }
        fields.Add(current.ToString());
        return [.. fields];
    }
}

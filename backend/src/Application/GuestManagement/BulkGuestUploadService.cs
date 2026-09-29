using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

/// <summary>
/// Processes a planner-uploaded CSV guest list and feeds each valid guest into the
/// same registration pipeline used by the public self-registration form.
///
/// Entry point for the new upload path:
///   Planner CSV upload -> BulkGuestUploadService -> Guest + RegistrationSubmission (PENDING_AI)
///                                                 -> GuestAiReviewWorker (existing)
///                                                 -> capacity / waitlist (existing)
///                                                 -> invitation / RSVP / QR (existing)
///
/// The public self-registration path is NOT modified.
/// </summary>
public class BulkGuestUploadService(
    IGuestRegistrationRepository repository,
    IRegistrationTokenGenerator tokens,
    TimeProvider clock)
{
    private const int MaxRows = 1000;

    /// <summary>
    /// Parses csvContent, validates each row, and creates Guest + RegistrationSubmission
    /// records for every valid, non-duplicate guest with status PENDING_AI.
    /// </summary>
    public async Task<BulkGuestUploadResult> ProcessAsync(
        Guid eventId,
        string plannerId,
        Stream fileStream,
        string extension,
        CancellationToken ct)
    {
        var (parsedRows, parseErrors) = extension switch
        {
            ".pdf" => GuestDocumentParser.ParsePdf(fileStream),
            ".docx" => GuestDocumentParser.ParseDocx(fileStream),
            _ => GuestCsvParser.Parse(new StreamReader(fileStream).ReadToEnd())
        };

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

        var successCount = 0;
        var alreadyRegisteredCount = 0;
        var dbErrors = new List<GuestUploadRowError>();

        foreach (var (row, details) in candidateRows)
        {
            if (ct.IsCancellationRequested) break;

            bool alreadyExists;
            try
            {
                alreadyExists = await repository.WithEventLockAsync(eventId, async eventDetails =>
                {
                    RequireOwner(eventDetails, plannerId);

                    var form = await repository.FindFormAsync(eventId, ct)
                        ?? throw new RegistrationException(409, "form_required",
                            "The event must have a registration form before uploading guests.");

                    if (form.Status != RegistrationFormStatus.PUBLISHED)
                        throw new RegistrationException(409, "form_not_published",
                            "The registration form must be published before uploading guests.");

                    var normalizedEmail = details.EmailAddress.ToUpperInvariant();
                    if (await repository.EmailExistsAsync(eventId, normalizedEmail, ct))
                        return true;

                    var now = clock.GetUtcNow();
                    var secret = tokens.Generate();
                    var guest = new Guest
                    {
                        EventId = eventId,
                        FullName = details.FullName,
                        EmailAddress = details.EmailAddress,
                        NormalizedEmail = normalizedEmail,
                        Organisation = details.Organisation,
                        PhoneNumber = details.PhoneNumber,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                    var submission = new RegistrationSubmission
                    {
                        EventId = eventId,
                        RegistrationFormId = form.Id,
                        RegistrationForm = form,
                        GuestId = guest.Id,
                        Guest = guest,
                        PublicReference = await UniqueTokenAsync(ct),
                        StatusSecretHash = tokens.Hash(secret),
                        RegisteredAt = now,
                        UpdatedAt = now,
                        Status = RegistrationStatus.PENDING_AI,
                        Answers = [],
                    };
                    repository.AddRegistration(submission);
                    return false;
                }, ct);
            }
            catch (RegistrationException ex) when (ex.Code is "form_required" or "form_not_published" or "not_found")
            {
                throw;
            }
            catch (RegistrationException ex)
            {
                dbErrors.Add(new GuestUploadRowError(row.RowNumber, "emailAddress", ex.Message));
                continue;
            }

            if (alreadyExists)
                alreadyRegisteredCount++;
            else
                successCount++;
        }

        var allErrors = parseErrors
            .Concat(duplicateRows)
            .Concat(dbErrors)
            .OrderBy(e => e.RowNumber)
            .ToList();

        var totalRows = parsedRows.Count + parseErrors.Count + duplicateRows.Count;

        return new BulkGuestUploadResult(
            TotalRows: totalRows,
            SuccessfulRows: successCount,
            FailedRows: parseErrors.Count + dbErrors.Count,
            DuplicateRows: duplicateRows.Count,
            AlreadyRegisteredRows: alreadyRegisteredCount,
            Errors: allErrors);
    }

    private async Task<string> UniqueTokenAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var token = tokens.Generate();
            if (!await repository.TokenExistsAsync(token, ct)) return token;
        }
        throw new RegistrationException(503, "token_generation_failed",
            "Unable to create a unique registration reference. Please retry.");
    }

    private static void RequireOwner(Event eventDetails, string plannerId)
    {
        if (string.IsNullOrWhiteSpace(plannerId) || eventDetails.OwnerId.ToString() != plannerId)
            throw new RegistrationException(404, "not_found", "Registration resource not found.");
    }

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

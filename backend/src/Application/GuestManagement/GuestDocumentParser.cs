using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace Application.GuestManagement;

public static class GuestDocumentParser
{
    private const int MaxRows = 1000;
    private static readonly Regex EmailCandidateRegex =
        new(@"[^\s,;<>]+@[^\s,;<>]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static (List<GuestUploadRow> Rows, List<GuestUploadRowError> Errors) ParsePdf(
        Stream stream,
        ILogger? logger = null)
    {
        try
        {
            using var pdf = PdfDocument.Open(stream);
            var text = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                text.AppendLine(page.Text);
            }

            return ParseText(text.ToString());
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Unable to read uploaded PDF document.");
            return InvalidDocument("The PDF file is corrupted or could not be read.");
        }
    }

    public static (List<GuestUploadRow> Rows, List<GuestUploadRowError> Errors) ParseDocx(
        Stream stream,
        ILogger? logger = null)
    {
        try
        {
            using var document = WordprocessingDocument.Open(stream, false);
            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                return InvalidDocument("The DOCX file does not contain a document body.");
            }

            var lines = new List<string>();
            AppendBlockLines(body, lines);
            return ParseText(string.Join(Environment.NewLine, lines));
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Unable to read uploaded DOCX document.");
            return InvalidDocument("The DOCX file is corrupted or could not be read.");
        }
    }

    private static void AppendBlockLines(OpenXmlElement parent, ICollection<string> lines)
    {
        foreach (var child in parent.ChildElements)
        {
            if (child is Paragraph paragraph)
            {
                lines.Add(paragraph.InnerText);
            }
            else if (child is Table table)
            {
                foreach (var row in table.Elements<TableRow>())
                {
                    var cells = row.Elements<TableCell>().Select(GetCellText);
                    lines.Add(string.Join('\t', cells));
                }
            }
            else if (child.ChildElements.Count > 0)
            {
                AppendBlockLines(child, lines);
            }
        }
    }

    private static string GetCellText(OpenXmlElement cell)
    {
        var content = new StringBuilder();
        foreach (var child in cell.ChildElements)
        {
            if (child is Paragraph paragraph)
            {
                if (content.Length > 0) content.Append(' ');
                content.Append(paragraph.InnerText);
            }
            else if (child is Table nestedTable)
            {
                if (content.Length > 0) content.Append(' ');
                content.Append(string.Join(' ', nestedTable.Elements<TableRow>().Select(row =>
                    string.Join('\t', row.Elements<TableCell>().Select(GetCellText)))));
            }
            else if (child.ChildElements.Count > 0)
            {
                var nestedContent = GetCellText(child);
                if (!string.IsNullOrWhiteSpace(nestedContent))
                {
                    if (content.Length > 0) content.Append(' ');
                    content.Append(nestedContent);
                }
            }
        }

        return content.ToString().Trim();
    }

    private static (List<GuestUploadRow> Rows, List<GuestUploadRowError> Errors) ParseText(string text)
    {
        var rows = new List<GuestUploadRow>();
        var errors = new List<GuestUploadRowError>();
        var lines = text.Split(['\r', '\n'], StringSplitOptions.None);
        var rowNumber = 0;
        int? fullNameColumn = null;
        int? emailColumn = null;
        int? organisationColumn = null;
        int? phoneColumn = null;
        var processedLines = 0;

        foreach (var rawLine in lines)
        {
            rowNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0) continue;
            if (++processedLines > MaxRows + 1)
            {
                errors.Add(new GuestUploadRowError(
                    rowNumber,
                    "file",
                    $"File exceeds the maximum of {MaxRows} guest rows. Only the first {MaxRows} rows were processed."));
                break;
            }

            if (TryGetHeaderColumns(
                    rawLine,
                    out var nameIndex,
                    out var addressIndex,
                    out var organisationIndex,
                    out var phoneIndex,
                    out var hasNameHeader,
                    out var hasEmailHeader))
            {
                fullNameColumn = nameIndex;
                emailColumn = addressIndex;
                organisationColumn = organisationIndex;
                phoneColumn = phoneIndex;
                continue;
            }

            if (hasNameHeader && !hasEmailHeader)
            {
                errors.Add(new GuestUploadRowError(
                    rowNumber, "header", "The DOCX table is missing the required emailAddress column."));
                continue;
            }

            if (fullNameColumn.HasValue && emailColumn.HasValue && rawLine.Contains('\t'))
            {
                var fields = rawLine.Split('\t', StringSplitOptions.None);
                string? Field(int? index) => index.HasValue && index.Value < fields.Length
                    ? NullIfEmpty(fields[index.Value])
                    : null;

                var fullName = Field(fullNameColumn);
                var structuredEmail = Field(emailColumn);
                if (fullName is null)
                    errors.Add(new GuestUploadRowError(
                        rowNumber, "fullName", $"Row {rowNumber}: fullName is required."));
                if (structuredEmail is null)
                    errors.Add(new GuestUploadRowError(
                        rowNumber, "emailAddress", $"Row {rowNumber}: emailAddress is required."));
                if (fullName is not null && structuredEmail is not null)
                {
                    rows.Add(new GuestUploadRow(
                        rowNumber,
                        fullName,
                        structuredEmail,
                        Field(organisationColumn),
                        Field(phoneColumn)));
                }
                continue;
            }

            var match = EmailCandidateRegex.Match(line);
            if (!match.Success)
            {
                if (line.Contains('@'))
                {
                    errors.Add(new GuestUploadRowError(
                        rowNumber, "emailAddress", $"Row {rowNumber}: email address is invalid."));
                }
                else if (ContainsStructuredFields(rawLine))
                {
                    var rowNameCandidate = FirstNonEmptyField(rawLine);
                    if (!string.IsNullOrWhiteSpace(rowNameCandidate))
                    {
                        errors.Add(new GuestUploadRowError(
                            rowNumber, "emailAddress", $"Row {rowNumber}: emailAddress is required."));
                    }
                }
                continue;
            }

            var email = match.Value.TrimEnd('.', '!', '?', ':', ')', ']');
            var name = line.Remove(match.Index, match.Length).Trim();
            name = Regex.Replace(name, @"^[,;\t\s]+|[,;\t\s]+$", "");
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(new GuestUploadRowError(
                    rowNumber, "fullName", $"Row {rowNumber}: fullName is required."));
                continue;
            }

            rows.Add(new GuestUploadRow(rowNumber, name, email, null, null));
        }

        if (rows.Count == 0 && errors.Count == 0)
        {
            errors.Add(new GuestUploadRowError(
                0, "file", "No guest rows containing a name and email address were found."));
        }

        return (rows, errors);
    }

    private static bool TryGetHeaderColumns(
        string line,
        out int? fullNameColumn,
        out int? emailColumn,
        out int? organisationColumn,
        out int? phoneColumn,
        out bool hasNameHeader,
        out bool hasEmailHeader)
    {
        var fields = line.Split('\t', StringSplitOptions.TrimEntries);
        var normalized = fields
            .Select(field => Regex.Replace(field, @"[\s_]", "").ToLowerInvariant())
            .ToArray();
        fullNameColumn = FindColumn(normalized, "name", "fullname", "guestname");
        emailColumn = FindColumn(normalized, "email", "emailaddress");
        organisationColumn = FindColumn(normalized, "organisation", "organization", "company");
        phoneColumn = FindColumn(normalized, "phonenumber", "phone", "mobile");
        hasNameHeader = fullNameColumn.HasValue;
        hasEmailHeader = emailColumn.HasValue;
        return hasNameHeader && hasEmailHeader;
    }

    private static int? FindColumn(string[] fields, params string[] names)
    {
        for (var index = 0; index < fields.Length; index++)
        {
            if (names.Contains(fields[index], StringComparer.Ordinal))
                return index;
        }
        return null;
    }

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool ContainsStructuredFields(string line)
        => line.Contains('\t') || line.Contains(',') || line.Contains(';');

    private static string? FirstNonEmptyField(string line)
        => line.Split(['\t', ',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

    private static (List<GuestUploadRow> Rows, List<GuestUploadRowError> Errors) InvalidDocument(string message)
        => ([], [new GuestUploadRowError(0, "file", message)]);
}

using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;

namespace Application.GuestManagement;

public static class GuestDocumentParser
{
    private static readonly Regex EmailRegex = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);

    public static (List<GuestUploadRow> Rows, List<GuestUploadRowError> Errors) ParsePdf(Stream stream)
    {
        try
        {
            using var pdf = PdfDocument.Open(stream);
            var sb = new System.Text.StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return ParseText(sb.ToString());
        }
        catch (Exception ex)
        {
            var errors = new List<GuestUploadRowError> { new(0, "file", $"Failed to parse PDF: {ex.Message}") };
            return ([], errors);
        }
    }

    public static (List<GuestUploadRow> Rows, List<GuestUploadRowError> Errors) ParseDocx(Stream stream)
    {
        try
        {
            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null)
            {
                return ([], [new(0, "file", "DOCX is empty.")]);
            }

            var sb = new System.Text.StringBuilder();
            foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                sb.AppendLine(para.InnerText);
            }
            return ParseText(sb.ToString());
        }
        catch (Exception ex)
        {
            var errors = new List<GuestUploadRowError> { new(0, "file", $"Failed to parse DOCX: {ex.Message}") };
            return ([], errors);
        }
    }

    private static (List<GuestUploadRow> Rows, List<GuestUploadRowError> Errors) ParseText(string text)
    {
        var rows = new List<GuestUploadRow>();
        var errors = new List<GuestUploadRowError>();
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int rowNumber = 1;

        foreach (var line in lines)
        {
            var match = EmailRegex.Match(line);
            if (match.Success)
            {
                var email = match.Value;
                var restOfLine = line.Replace(email, "").Trim();
                restOfLine = Regex.Replace(restOfLine, @"^[,;\t\s]+|[,;\t\s]+$", "");
                var name = string.IsNullOrWhiteSpace(restOfLine) ? "Unknown" : restOfLine;
                rows.Add(new GuestUploadRow(rowNumber, name, email, null, null));
                rowNumber++;
            }
        }

        if (rows.Count == 0)
        {
            errors.Add(new GuestUploadRowError(0, "file", "No valid guest data (Name, Email) found in the document."));
        }
        return (rows, errors);
    }
}

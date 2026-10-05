using System.Text;
using Application.GuestManagement;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Backend.UnitTests;

public class GuestDocumentParserTests
{
    [Fact]
    public void ParseDocxReadsTableParagraphAndNestedTableRowsAndSkipsBlankRows()
    {
        var nestedTable = new Table(
            Row("Nested Guest", "nested@example.test"));
        var document = CreateDocx(
            new Paragraph(new Run(new Text("Paragraph Guest paragraph@example.test"))),
            new Table(
                Row("Full Name", "Email Address", "Organisation", "Phone Number"),
                Row("Table Guest", "table@example.test", "SLIIT", "+94123456789"),
                new TableRow(new TableCell(new Paragraph()), new TableCell(new Paragraph())),
                new TableRow(new TableCell(nestedTable))));

        var (rows, errors) = GuestDocumentParser.ParseDocx(document);

        Assert.Empty(errors);
        Assert.Equal(3, rows.Count);
        Assert.Contains(rows, row => row.FullName == "Paragraph Guest");
        Assert.Contains(rows, row => row.FullName == "Table Guest");
        Assert.Contains(rows, row => row.FullName == "Nested Guest");
        var tableGuest = Assert.Single(rows, row => row.FullName == "Table Guest");
        Assert.Equal("SLIIT", tableGuest.Organisation);
        Assert.Equal("+94123456789", tableGuest.PhoneNumber);
    }

    [Fact]
    public void ParseDocxReportsRowsWithoutEmailsAndIgnoresBlankRows()
    {
        var document = CreateDocx(
            new Table(
                Row("Full Name", "Email Address"),
                Row("Valid Guest", "valid@example.test"),
                Row("Missing Email", "")));

        var (rows, errors) = GuestDocumentParser.ParseDocx(document);

        Assert.Single(rows);
        var error = Assert.Single(errors);
        Assert.Equal("emailAddress", error.Field);
        Assert.Contains("emailAddress is required", error.Message);
    }

    [Fact]
    public void ParseDocxReturnsFileErrorForCorruptedContent()
    {
        using var document = new MemoryStream([0x00, 0x01, 0x02]);

        var (rows, errors) = GuestDocumentParser.ParseDocx(document);

        Assert.Empty(rows);
        Assert.Equal("file", Assert.Single(errors).Field);
    }

    [Fact]
    public void ParsePdfExtractsGuestNameAndEmail()
    {
        using var document = new MemoryStream(CreatePdf("PDF Guest pdf@example.test"));

        var (rows, errors) = GuestDocumentParser.ParsePdf(document);

        Assert.Empty(errors);
        var row = Assert.Single(rows);
        Assert.Equal("PDF Guest", row.FullName);
        Assert.Equal("pdf@example.test", row.EmailAddress);
    }

    private static TableRow Row(string name, string email, params string[] optionalValues)
        => new(
            new[]
            {
                new TableCell(new Paragraph(new Run(new Text(name)))),
                new TableCell(new Paragraph(new Run(new Text(email)))),
            }
            .Concat(optionalValues.Select(value => new TableCell(new Paragraph(new Run(new Text(value))))))
            .ToArray());

    private static MemoryStream CreateDocx(params OpenXmlElement[] blocks)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(blocks));
            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static byte[] CreatePdf(string text)
    {
        var content = $"BT /F1 12 Tf 72 720 Td ({text}) Tj ET";
        var contentLength = Encoding.ASCII.GetByteCount(content);
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {contentLength} >>\nstream\n{content}\nendstream",
        };
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.NewLine = "\n";
        writer.WriteLine("%PDF-1.4");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            writer.WriteLine($"{index + 1} 0 obj");
            writer.WriteLine(objects[index]);
            writer.WriteLine("endobj");
            writer.Flush();
        }

        var xrefOffset = stream.Position;
        writer.WriteLine($"xref\n0 {objects.Length + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        writer.WriteLine($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>");
        writer.WriteLine($"startxref\n{xrefOffset}\n%%EOF");
        writer.Flush();
        return stream.ToArray();
    }
}

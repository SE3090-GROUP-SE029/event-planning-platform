using Application.GuestManagement;

namespace Backend.UnitTests;

/// <summary>
/// Unit tests for GuestCsvParser (CSV parsing utilities for the bulk guest list upload flow).
/// No I/O, no database, no DI — tests pure in-process logic only.
/// </summary>
public class BulkGuestUploadCsvTests
{
    // ── SplitLine ─────────────────────────────────────────────────────────────

    [Fact]
    public void SplitLine_SplitsSimpleCommaRow()
    {
        var result = GuestCsvParser.SplitLine("Alice,alice@example.com,SLIIT,+94123");
        Assert.Equal(["Alice", "alice@example.com", "SLIIT", "+94123"], result);
    }

    [Fact]
    public void SplitLine_HandlesQuotedFieldWithComma()
    {
        var result = GuestCsvParser.SplitLine("\"Smith, Alice\",alice@example.com,,");
        Assert.Equal(["Smith, Alice", "alice@example.com", "", ""], result);
    }

    [Fact]
    public void SplitLine_HandlesEscapedQuote()
    {
        var result = GuestCsvParser.SplitLine("\"O\"\"Brien\",ob@example.com");
        Assert.Equal(["O\"Brien", "ob@example.com"], result);
    }

    [Fact]
    public void SplitLine_HandlesEmptyFields()
    {
        var result = GuestCsvParser.SplitLine("Alice,,");
        Assert.Equal(["Alice", "", ""], result);
    }

    // ── Parse ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyString_ReturnsFileError()
    {
        var (rows, errors) = GuestCsvParser.Parse("   ");
        Assert.Empty(rows);
        Assert.Single(errors);
        Assert.Equal("file", errors[0].Field);
    }

    [Fact]
    public void Parse_MissingRequiredHeader_ReturnsHeaderError()
    {
        var csv = "fullName,organisation\nAlice,SLIIT\n";
        var (rows, errors) = GuestCsvParser.Parse(csv);
        Assert.Empty(rows);
        Assert.Single(errors);
        Assert.Equal("header", errors[0].Field);
    }

    [Fact]
    public void Parse_ValidMinimalCsv_ReturnsParsedRows()
    {
        var csv = "fullName,emailAddress\nAlice,alice@example.com\nBob,bob@example.com\n";
        var (rows, errors) = GuestCsvParser.Parse(csv);
        Assert.Empty(errors);
        Assert.Equal(2, rows.Count);
        Assert.Equal("Alice", rows[0].FullName);
        Assert.Equal("alice@example.com", rows[0].EmailAddress);
        Assert.Equal(2, rows[0].RowNumber);
        Assert.Equal(3, rows[1].RowNumber);
    }

    [Fact]
    public void Parse_ValidFullCsv_ParsesOptionalColumns()
    {
        var csv = "fullName,emailAddress,organisation,phoneNumber\nAlice,alice@example.com,SLIIT,+94123\n";
        var (rows, errors) = GuestCsvParser.Parse(csv);
        Assert.Empty(errors);
        Assert.Single(rows);
        Assert.Equal("SLIIT", rows[0].Organisation);
        Assert.Equal("+94123", rows[0].PhoneNumber);
    }

    [Fact]
    public void Parse_HeaderCaseInsensitiveAndSpaceTolerant()
    {
        var csv = "Full Name,Email Address,Organisation,Phone Number\nAlice,alice@example.com,,\n";
        var (rows, errors) = GuestCsvParser.Parse(csv);
        Assert.Empty(errors);
        Assert.Single(rows);
    }

    [Fact]
    public void Parse_EmptyNameField_ReturnsRowError()
    {
        var csv = "fullName,emailAddress\n,alice@example.com\n";
        var (rows, errors) = GuestCsvParser.Parse(csv);
        Assert.Empty(rows);
        Assert.Single(errors);
        Assert.Equal("fullName", errors[0].Field);
        Assert.Equal(2, errors[0].RowNumber);
    }

    [Fact]
    public void Parse_EmptyEmailField_ReturnsRowError()
    {
        var csv = "fullName,emailAddress\nAlice,\n";
        var (rows, errors) = GuestCsvParser.Parse(csv);
        Assert.Empty(rows);
        Assert.Single(errors);
        Assert.Equal("emailAddress", errors[0].Field);
    }

    [Fact]
    public void Parse_SkipsBlankLines()
    {
        var csv = "fullName,emailAddress\n\nAlice,alice@example.com\n\n";
        var (rows, errors) = GuestCsvParser.Parse(csv);
        Assert.Empty(errors);
        Assert.Single(rows);
    }

    [Fact]
    public void Parse_CrLfLineEndings_Handled()
    {
        var csv = "fullName,emailAddress\r\nAlice,alice@example.com\r\n";
        var (rows, errors) = GuestCsvParser.Parse(csv);
        Assert.Empty(errors);
        Assert.Single(rows);
        Assert.Equal("Alice", rows[0].FullName);
    }

    [Fact]
    public void Parse_RowsExceedingMaximum_SurfacesError()
    {
        var sb = new System.Text.StringBuilder("fullName,emailAddress\n");
        for (var i = 0; i <= 1001; i++)
            sb.AppendLine($"Guest {i},guest{i}@example.com");
        var (rows, errors) = GuestCsvParser.Parse(sb.ToString());
        Assert.Equal(1000, rows.Count); // capped at MaxRows
        Assert.Single(errors);
        Assert.Equal("file", errors[0].Field);
    }

    [Fact]
    public void Parse_WhitespaceOnlyOptionalFields_YieldNullOrganisationAndPhone()
    {
        var csv = "fullName,emailAddress,organisation,phoneNumber\nAlice,alice@example.com,   ,   \n";
        var (rows, errors) = GuestCsvParser.Parse(csv);
        Assert.Empty(errors);
        Assert.Single(rows);
        Assert.Null(rows[0].Organisation);
        Assert.Null(rows[0].PhoneNumber);
    }
}

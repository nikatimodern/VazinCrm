using ClosedXML.Excel;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Services;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxationReportWorkbookTests
{
    [Fact]
    public void ExportPreservesIdentifiersAndExactLargeAmountsAsText()
    {
        var row = new TaxReportRow(Guid.Empty, null, 44, "=1+1", new DateTime(2026, 1, 5), 123456789012345678m,
            "ریال", TaxDispatchKind.Original, "000005", "ABC123000100000000014", TaxReportStatus.Ready, false);
        var file = TaxationReportWorkbook.Export(new([row, row with { Amount = 123456.25m }], [], 0, 0, 0, 1, 1, 2),
            TaxReportScope.Sandbox);
        using var workbook = new XLWorkbook(new MemoryStream(file.FileContents));
        var sheet = workbook.Worksheet(1);
        Assert.True(sheet.RightToLeft);
        Assert.False(sheet.Cell("C2").HasFormula);
        Assert.Equal("=1+1", sheet.Cell("C2").GetString());
        Assert.Equal("123456789012345678", sheet.Cell("E2").GetString());
        Assert.Equal(123456.25, sheet.Cell("E3").GetDouble());
        Assert.Equal("000005", sheet.Cell("H2").GetString());
        Assert.Equal("آزمایشی سازمان", sheet.Cell("L2").GetString());
    }
}

using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxSettlementTests
{
    private static readonly TaxSigningBinding Binding = new(TaxAuthorityEnvironment.Sandbox, new string('A', 64), "14003778990");

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void DomesticSettlementUsesPrincipalBeforeVat(int method)
    {
        var preview = TaxProtocolTests.Preview(Guid.NewGuid());
        var shares = method == 3 ? new TaxSettlementShares(10000, 9500) : null;
        using var packet = JsonDocument.Parse(TaxInvoicePayload.Build(preview, Binding,
            new(49321217, TaxProtocolTests.Issued, 2, method, shares)).Json);
        var header = packet.RootElement.GetProperty("header");
        Assert.Equal(method, header.GetProperty("setm").GetInt32());
        Assert.Equal(21255, header.GetProperty("tbill").GetInt32());
        if (method == 3)
        {
            Assert.Equal(10000m, header.GetProperty("cap").GetDecimal());
            Assert.Equal(9500m, header.GetProperty("insp").GetDecimal());
        }
        else
        {
            Assert.False(header.TryGetProperty("cap", out _));
            Assert.False(header.TryGetProperty("insp", out _));
        }
    }

    [Theory]
    [InlineData(3, "-1", "19501")]
    [InlineData(3, "0", "19500")]
    [InlineData(3, "10000.5", "9499.5")]
    [InlineData(3, "10000", "11255")]
    [InlineData(3, "10000", "1")]
    [InlineData(1, "10000", "9500")]
    [InlineData(2, "10000", "9500")]
    [InlineData(4, "10000", "9500")]
    public void InvalidMethodsAndSharesCannotBuildPayload(int method, string cash, string credit)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var filing = new TaxFilingDetails(1, TaxProtocolTests.Issued, 2, method,
            new(decimal.Parse(cash, culture), decimal.Parse(credit, culture)));
        Assert.Throws<ArgumentException>(() => TaxInvoicePayload.Build(TaxProtocolTests.Preview(Guid.NewGuid()), Binding, filing));
    }
}

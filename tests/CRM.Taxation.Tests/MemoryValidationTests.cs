using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemory;
using CRM.Core.ApplicationServices.Taxation.Commands.SetTaxProductMapping;
using Xunit;

namespace CRM.Taxation.Tests;

public class MemoryValidationTests
{
    [Theory]
    [InlineData("AB12", "Test")]
    [InlineData("ABC۱۲۳", "Test")]
    [InlineData("ABC123", " ")]
    public async Task InvalidMemoryIsRejectedBeforePersistence(string fiscalId, string title)
    {
        var handler = new SaveTaxMemoryCommandHandler(null);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new(null, fiscalId, title), default));
    }

    [Theory]
    [InlineData("123", "164")]
    [InlineData("1234567890123", "kg")]
    public async Task InvalidMappingIsRejectedBeforePersistence(string goodsId, string unit)
    {
        var handler = new SetTaxProductMappingCommandHandler(null);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new(Guid.NewGuid(), 1, goodsId, unit), default));
    }
}

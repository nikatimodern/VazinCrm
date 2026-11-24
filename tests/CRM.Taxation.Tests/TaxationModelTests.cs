using CRM.Core.Domain.Taxation;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRM.Taxation.Tests;

public class TaxationModelTests
{
    [Fact]
    public void SqlModelEnforcesOneActiveMemoryAndPreservesHistoricalLinks()
    {
        var options = new DbContextOptionsBuilder<CRMContextDb>().UseSqlServer("Server=unused;Database=model_only").Options;
        using var db = new CRMContextDb(options);
        var memory = db.Model.FindEntityType(typeof(TaxMemory));
        Assert.Contains(memory.GetIndexes(), index => index.IsUnique && index.GetFilter() == "[IsActive] = 1");
        Assert.Contains(memory.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == "FiscalId");
        var snapshot = db.Model.FindEntityType(typeof(TaxInvoiceSnapshot));
        Assert.Equal(2, snapshot.GetForeignKeys().Count());
        Assert.All(snapshot.GetForeignKeys(), key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));
    }
}

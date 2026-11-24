using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxInvoice;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxPreview;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Taxation;
using CRM.Infrastructures.Sql;
using CRM.Infrastructures.Sql.Taxation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxationSqlFactAttribute : FactAttribute
{
    public TaxationSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("VAZIN_TAX_LOCALDB_TESTS") != "1")
            Skip = "Requires the dedicated VazinTaxationStage2Tests LocalDB instance.";
    }
}

public sealed class TaxationSqlTests : IAsyncLifetime
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private readonly DbContextOptions<CRMContextDb> options = new DbContextOptionsBuilder<CRMContextDb>()
        .UseSqlServer($@"Server=(localdb)\VazinTaxationStage2Tests;Database=TaxationTests_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true")
        .Options;
    private readonly ITaxPreviewProtector protection = new TestProtection();

    private CRMContextDb Open() => new(options);

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("VAZIN_TAX_LOCALDB_TESTS") != "1") return;
        await using var db = Open();
        Assert.True(await db.Database.EnsureCreatedAsync());
    }

    public async Task DisposeAsync()
    {
        if (Environment.GetEnvironmentVariable("VAZIN_TAX_LOCALDB_TESTS") != "1") return;
        await using var db = Open();
        await db.Database.EnsureDeletedAsync();
    }

    [TaxationSqlFact]
    public async Task MemoryActivationAndUniqueConstraintsSurviveConcurrentWriters()
    {
        await using var left = Open();
        await using var right = Open();
        var a = new TaxationRepository(left, protection);
        var b = new TaxationRepository(right, protection);
        var first = await a.SaveMemoryAsync(null, "ABC123", "First", Ct);
        var second = await b.SaveMemoryAsync(null, "DEF123", "Second", Ct);
        await Task.WhenAll(a.ActivateAsync(first, Ct), b.ActivateAsync(second, Ct));
        Assert.Single(await left.TaxMemories.AsNoTracking().Where(x => x.IsActive).ToListAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => b.ActivateAsync(Guid.NewGuid(), Ct));
        Assert.Single(await right.TaxMemories.AsNoTracking().Where(x => x.IsActive).ToListAsync());

        async Task<bool> InsertDuplicateCandidate()
        {
            await using var writer = Open();
            try { await new TaxationRepository(writer, protection).SaveMemoryAsync(null, "DUP123", "Duplicate", Ct); return true; }
            catch (ArgumentException) { return false; }
        }
        var results = await Task.WhenAll(InsertDuplicateCandidate(), InsertDuplicateCandidate());
        Assert.Single(results.Where(x => x));
        var error = await Assert.ThrowsAsync<SqlException>(() =>
            left.TaxMemories.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, true)));
        Assert.Equal(2601, error.Number);
        Assert.Single(await left.TaxMemories.AsNoTracking().Where(x => x.IsActive).ToListAsync());
    }

    [TaxationSqlFact]
    public async Task SnapshotRejectsStaleVersionAndHoldsInvoiceReadLocksThroughSave()
    {
        await using var db = Open();
        var (factor, _) = await SeedAsync(db, FactorStatus.تایید_شده);
        var repo = new TaxationRepository(db, protection);
        var handler = new SaveTaxPreviewCommandHandler(repo);
        var original = await PreviewVersion(repo, factor);
        await using var editor = Open();
        await editor.Factors.Where(x => x.Id == factor).ExecuteUpdateAsync(s => s.SetProperty(x => x.Customer, "Edited"));
        await Assert.ThrowsAsync<TaxPreviewConflictException>(() => handler.HandleAsync(new(factor, "tester", original), Ct));
        Assert.Empty(await db.TaxInvoiceSnapshots.ToListAsync());

        using var release = new ManualResetEventSlim();
        var captured = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var save = Task.Run(() => repo.SaveSnapshotAsync(factor, "tester", source =>
        {
            captured.SetResult();
            Assert.True(release.Wait(TimeSpan.FromSeconds(15)));
            return source.Factor.Customer;
        }, Ct));
        try
        {
            await captured.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await editor.Database.OpenConnectionAsync();
            await editor.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 500");
            var error = await Record.ExceptionAsync(() => editor.Factors.Where(x => x.Id == factor)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Customer, "Concurrent")));
            Assert.Equal(1222, Assert.IsType<SqlException>(error?.GetBaseException()).Number);
        }
        finally { release.Set(); }
        Assert.Equal("Edited", await repo.ReadSnapshotAsync(await save, Ct));
    }

    [TaxationSqlFact]
    public async Task EnqueueFreezesMemoryAndPayloadAndRecoversAmbiguousAttempt()
    {
        await using var firstProcess = Open();
        var (factor, memory) = await SeedAsync(firstProcess, FactorStatus.در_دست_تایید);
        var repo = new TaxationRepository(firstProcess, protection);
        var enqueue = new EnqueueTaxInvoiceCommandHandler(repo);
        var pendingVersion = await PreviewVersion(repo, factor);
        await Assert.ThrowsAsync<ArgumentException>(() => enqueue.HandleAsync(new(factor, "tester", pendingVersion), Ct));
        Assert.Empty(await firstProcess.TaxDispatches.ToListAsync());

        await firstProcess.Factors.Where(x => x.Id == factor)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.status, FactorStatus.تایید_شده));
        var version = await PreviewVersion(repo, factor);
        await using var secondProcess = Open();
        var other = new TaxationRepository(secondProcess, protection);
        var ids = await Task.WhenAll(
            enqueue.HandleAsync(new(factor, "tester", version), Ct),
            new EnqueueTaxInvoiceCommandHandler(other).HandleAsync(new(factor, "tester", version), Ct));
        Assert.Equal(ids[0], ids[1]);
        Assert.Single(await secondProcess.TaxInvoiceSnapshots.ToListAsync());

        var replacement = await other.SaveMemoryAsync(null, "XYZ123", "Replacement", Ct);
        await other.ActivateAsync(replacement, Ct);
        await secondProcess.Factors.Where(x => x.Id == factor).ExecuteUpdateAsync(s => s.SetProperty(x => x.Customer, "Changed"));
        var stored = await secondProcess.TaxDispatches.AsNoTracking().SingleAsync();
        Assert.True(stored.MemoryWasActive);
        Assert.Equal("Original", stored.MemoryTitle);
        var claim = await Task.WhenAll(
            new TaxDispatchQueue(firstProcess, protection).ClaimAsync(TimeSpan.FromMinutes(1), Ct),
            new TaxDispatchQueue(secondProcess, protection).ClaimAsync(TimeSpan.FromMinutes(1), Ct));
        var attempt = Assert.Single(claim.OfType<TaxDispatchAttempt>());
        Assert.Equal(memory, attempt.MemoryId);
        Assert.Equal("ABC123", attempt.FiscalId);
        Assert.Equal("Original buyer", System.Text.Json.JsonSerializer.Deserialize<InvoicePreview>(attempt.Preview).Buyer);

        await secondProcess.TaxDispatches.ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseUntilUtc, DateTime.UtcNow.AddMinutes(-1)));
        await using var restarted = Open();
        var queue = new TaxDispatchQueue(restarted, protection);
        Assert.Null(await queue.ClaimAsync(TimeSpan.FromMinutes(1), Ct));
        Assert.Equal(TaxDispatchStatus.Unknown, await restarted.TaxDispatches.Select(x => x.Status).SingleAsync());
        Assert.False(await queue.CompleteAsync(attempt.DispatchId, Guid.NewGuid(), TaxDispatchStatus.Accepted, Ct));
        Assert.True(await queue.CompleteAsync(attempt.DispatchId, attempt.AttemptId, TaxDispatchStatus.Accepted, Ct));
        Assert.Null(await queue.ClaimAsync(TimeSpan.FromMinutes(1), Ct));
    }

    [TaxationSqlFact]
    public async Task InvalidQueueRowsAndDefaultMappingAreRejectedAndSnapshotIsRolledBack()
    {
        await using var db = Open();
        var (factor, _) = await SeedAsync(db, FactorStatus.تایید_شده);
        var repo = new TaxationRepository(db, protection);
        var version = await PreviewVersion(repo, factor);
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE [TaxDispatches] ADD CONSTRAINT [CK_TestRejectQueue] CHECK ([Status] <> 0);
            """);
        var rejected = await Assert.ThrowsAsync<DbUpdateException>(() => new EnqueueTaxInvoiceCommandHandler(repo)
            .HandleAsync(new(factor, "tester", version), Ct));
        Assert.Equal(547, Assert.IsType<SqlException>(rejected.InnerException).Number);

        await using var verify = Open();
        Assert.Empty(await verify.TaxDispatches.ToListAsync());
        Assert.Empty(await verify.TaxInvoiceSnapshots.ToListAsync());
        await verify.Database.ExecuteSqlRawAsync("ALTER TABLE [TaxDispatches] DROP CONSTRAINT [CK_TestRejectQueue]");
        await new EnqueueTaxInvoiceCommandHandler(new TaxationRepository(verify, protection))
            .HandleAsync(new(factor, "tester", version), Ct);
        var invalidState = await Assert.ThrowsAsync<SqlException>(() => verify.TaxDispatches
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, TaxDispatchStatus.InFlight)));
        Assert.Equal(547, invalidState.Number);
        var mapping = await verify.TaxProductMappings.AsNoTracking().SingleAsync();
        mapping.Id = Guid.NewGuid();
        mapping.GoodsId = "9999999999999";
        verify.TaxProductMappings.Add(mapping);
        await Assert.ThrowsAsync<DbUpdateException>(() => verify.SaveChangesAsync());
        verify.ChangeTracker.Clear();
        mapping.Id = Guid.NewGuid();
        mapping.ProductId = Guid.NewGuid();
        verify.TaxProductMappings.Add(mapping);
        var fk = await Assert.ThrowsAsync<DbUpdateException>(() => verify.SaveChangesAsync());
        Assert.Equal(547, Assert.IsType<SqlException>(fk.InnerException).Number);
    }

    private async Task<(Guid Factor, Guid Memory)> SeedAsync(CRMContextDb db, FactorStatus status)
    {
        var repo = new TaxationRepository(db, protection);
        var memory = await repo.SaveMemoryAsync(null, "ABC123", "Original", Ct);
        await repo.ActivateAsync(memory, Ct);
        var product = new Product { Id = Guid.NewGuid() };
        db.Products.Add(product);
        var preFactor = new PreFactor { Id = Guid.NewGuid() };
        db.PreFactors.Add(preFactor);
        db.ASettings.Add(new ASetting { Id = Guid.NewGuid(), TaxAfterDiscount = true });
        var factor = new Factor
        {
            Id = Guid.NewGuid(), PfactorID = preFactor.Id, DateMiladi = DateTime.Today, CustomerId = Guid.NewGuid(), Customer = "Original buyer",
            Cus_EghtesadiCode = "123", OfficialType = 1, status = status, PercentTax = 10,
            SumTotalAmount = 100, Discount = 5, TotalTax = 9, PaymentAmount = 104,
            FactorItems = [new FactorItem { Id = Guid.NewGuid(), Row = 1, ProductCode = product.Id,
                WeightCount = "1", UnitAmount = 100, Discount = 5, TaxAmount = 9, TotalAmount = 104 }]
        };
        db.Factors.Add(factor);
        await db.SaveChangesAsync();
        await repo.SetMappingAsync(product.Id, 0, "1234567890123", "164", Ct);
        db.ChangeTracker.Clear();
        return (factor.Id, memory);
    }

    private static async Task<string> PreviewVersion(TaxationRepository repo, Guid factor) =>
        InvoicePreview.Build(await repo.GetPreviewSourceAsync(factor, Ct)).Version;

    private sealed class TestProtection : ITaxPreviewProtector
    {
        public string Protect(string preview) => "test:" + preview;
        public string Unprotect(string preview) => preview[5..];
    }
}

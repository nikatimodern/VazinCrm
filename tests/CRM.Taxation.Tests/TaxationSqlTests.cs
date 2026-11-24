using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxPayment;
using CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxPayment;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.ApplicationServices.Taxation.Queries.PreviewTaxInvoice;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxReference;
using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxInvoice;
using CRM.Core.ApplicationServices.Taxation.Commands.InquireTaxSubmission;
using CRM.Core.ApplicationServices.Taxation.Commands.InquireTaxWorkFolder;
using CRM.Core.ApplicationServices.Taxation.Submission;
using System.Security.Cryptography;
using System.Diagnostics;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxInvoice;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxPreview;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Composition;
using CRM.Infrastructures.Sql;
using CRM.Infrastructures.Sql.Taxation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
        var foundation = db.Database.GetMigrations().Single(x => x.EndsWith("_AddTaxationFoundation", StringComparison.Ordinal));
        await db.GetService<IMigrator>().MigrateAsync(foundation);
        var memory = new TaxMemory { Id = Guid.NewGuid(), FiscalId = "KEEP01", Title = "Before queue", UpdatedUtc = DateTime.UtcNow };
        db.TaxMemories.Add(memory);
        await db.SaveChangesAsync();
        await db.Database.MigrateAsync();
        Assert.Equal("Before queue", await db.TaxMemories.Where(x => x.Id == memory.Id).Select(x => x.Title).SingleAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
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
            new TaxDispatchQueue(firstProcess, protection).ClaimAsync(ids[0], TimeSpan.FromMinutes(1), Ct),
            new TaxDispatchQueue(secondProcess, protection).ClaimAsync(ids[0], TimeSpan.FromMinutes(1), Ct));
        var attempt = Assert.Single(claim.OfType<TaxDispatchAttempt>());
        Assert.Equal(memory, attempt.MemoryId);
        Assert.Equal("ABC123", attempt.FiscalId);
        Assert.Equal("Original buyer", System.Text.Json.JsonSerializer.Deserialize<InvoicePreview>(attempt.Preview).Buyer);

        await secondProcess.TaxDispatches.ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseUntilUtc, DateTime.UtcNow.AddMinutes(-1)));
        await using var restarted = Open();
        var queue = new TaxDispatchQueue(restarted, protection);
        Assert.Equal(new TaxDispatchState(ids[0], attempt.AttemptId, TaxDispatchStatus.Unknown),
            await queue.GetStateAsync(ids[0], Ct));
        Assert.Null(await queue.ClaimAsync(ids[0], TimeSpan.FromMinutes(1), Ct));
        Assert.False(await queue.CompleteAsync(attempt.DispatchId, Guid.NewGuid(), TaxDispatchStatus.Accepted, Ct));
        Assert.False(await queue.CompleteAsync(attempt.DispatchId, attempt.AttemptId, TaxDispatchStatus.Unknown, Ct));
        Assert.True(await queue.CompleteAsync(attempt.DispatchId, attempt.AttemptId, TaxDispatchStatus.Accepted, Ct));
        Assert.False(await queue.CompleteAsync(attempt.DispatchId, attempt.AttemptId, TaxDispatchStatus.Rejected, Ct));
        Assert.Equal(TaxDispatchStatus.Accepted, (await queue.GetStateAsync(attempt.DispatchId, Ct)).Status);
        Assert.Null(await queue.ClaimAsync(ids[0], TimeSpan.FromMinutes(1), Ct));
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

    [TaxationSqlFact]
    public async Task OriginalUniquenessDoesNotBlockReferencedDocuments()
    {
        await using var db = Open();
        var (factor, _) = await SeedAsync(db, FactorStatus.تایید_شده);
        var repo = new TaxationRepository(db, protection);
        var version = await PreviewVersion(repo, factor);
        var originalId = await new EnqueueTaxInvoiceCommandHandler(repo).HandleAsync(new(factor, "tester", version), Ct);
        var original = await db.TaxDispatches.AsNoTracking().SingleAsync();
        var snapshot = await new SaveTaxPreviewCommandHandler(repo).HandleAsync(new(factor, "tester", version), Ct);
        var candidate = new TaxDispatch
        {
            Id = Guid.NewGuid(), FactorId = factor, SnapshotId = snapshot, MemoryId = original.MemoryId,
            FiscalId = original.FiscalId, MemoryTitle = original.MemoryTitle, MemoryUpdatedUtc = original.MemoryUpdatedUtc,
            MemoryWasActive = original.MemoryWasActive, QueuedUtc = DateTime.UtcNow
        };
        db.TaxDispatches.Add(candidate);
        var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(2601, Assert.IsType<SqlException>(duplicate.InnerException).Number);
        candidate.Kind = TaxDispatchKind.Correction;
        var missingReference = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(547, Assert.IsType<SqlException>(missingReference.InnerException).Number);
        candidate.ReferencedDispatchId = originalId;
        await db.SaveChangesAsync();
        Assert.Equal(2, await db.TaxDispatches.CountAsync());
    }

    [TaxationSqlFact]
    public async Task PendingDispatchSurvivesLocalDbRestart()
    {
        Guid dispatchId;
        await using (var db = Open())
        {
            var (factor, _) = await SeedAsync(db, FactorStatus.تایید_شده);
            var repo = new TaxationRepository(db, protection);
            dispatchId = await new EnqueueTaxInvoiceCommandHandler(repo)
                .HandleAsync(new(factor, "tester", await PreviewVersion(repo, factor)), Ct);
        }

        SqlConnection.ClearAllPools();
        await RunLocalDbAsync("stop");
        await RunLocalDbAsync("start");

        await using var restarted = Open();
        var queue = new TaxDispatchQueue(restarted, protection);
        Assert.Equal(TaxDispatchStatus.Pending, (await queue.GetStateAsync(dispatchId, Ct)).Status);
        Assert.NotNull(await queue.ClaimAsync(dispatchId, TimeSpan.FromMinutes(1), Ct));
    }

    [TaxationSqlFact]
    public async Task LocalFailureStaysPendingAndTimeoutRequiresInquiryBeforeAnyResolution()
    {
        await using var db = Open();
        var id = await QueueSubmissionAsync(db);
        var gateway = new TestGateway { FailPrepare = true };
        var queue = new TaxDispatchQueue(db, protection);
        var repo = new TaxSubmissionRepository(db, protection);
        var bindings = new TestBindings();
        var submit = new SubmitTaxInvoiceCommandHandler(repo, queue, bindings, gateway, TimeProvider.System);
        var command = new SubmitTaxInvoiceCommand(id, Filing());
        var broken = new SubmitTaxInvoiceCommandHandler(new TaxSubmissionRepository(db, new UnreadableProtection()),
            queue, bindings, gateway, TimeProvider.System);
        await Assert.ThrowsAsync<CryptographicException>(() => broken.HandleAsync(command, Ct));
        Assert.Null((await queue.GetStateAsync(id, Ct)).AttemptId);
        await Assert.ThrowsAsync<CryptographicException>(() => submit.HandleAsync(command, Ct));
        var stillPending = await queue.GetStateAsync(id, Ct);
        Assert.Equal(TaxDispatchStatus.Pending, stillPending.Status);
        Assert.Null(stillPending.AttemptId);

        gateway.FailPrepare = false;
        gateway.Timeout = true;
        bindings.Hash = new string('B', 64);
        var unknown = await submit.HandleAsync(command, Ct);
        Assert.Equal(new string('A', 64), gateway.PreparedCertificate);
        Assert.Equal(TaxDispatchStatus.Unknown, unknown.Status);
        Assert.Equal(1, gateway.Sends);
        Assert.Equal(unknown, await submit.HandleAsync(command, Ct));
        Assert.Equal(1, gateway.Sends);

        await using var restarted = Open();
        var restored = new TaxSubmissionRepository(restarted, protection);
        var restoredQueue = new TaxDispatchQueue(restarted, protection);
        var inquiry = new InquireTaxSubmissionCommandHandler(restored, restoredQueue, gateway);
        Assert.Equal(unknown, await inquiry.HandleAsync(new(id), Ct));
        Assert.Equal(unknown.AttemptId, gateway.InquiredAttempt);
        gateway.Result = new(Guid.NewGuid(), "FAILED", """{"error":[{"code":"mock"}]}""");
        Assert.Equal(TaxDispatchStatus.Rejected, (await inquiry.HandleAsync(new(id), Ct)).Status);
        Assert.Equal(1, gateway.Sends);
    }

    [TaxationSqlFact]
    public async Task ConcurrentSubmitSendsOnceAndSeparatesAcknowledgementProcessingAndWorkFolder()
    {
        await using var left = Open();
        await using var right = Open();
        var id = await QueueSubmissionAsync(left);
        var gateway = new TestGateway();
        var queue = new TaxDispatchQueue(left, protection);
        var repo = new TaxSubmissionRepository(left, protection);
        var a = new SubmitTaxInvoiceCommandHandler(repo, queue, new TestBindings(), gateway, TimeProvider.System);
        var b = new SubmitTaxInvoiceCommandHandler(new TaxSubmissionRepository(right, protection),
            new TaxDispatchQueue(right, protection), new TestBindings(), gateway, TimeProvider.System);
        var request = new SubmitTaxInvoiceCommand(id, Filing());
        await Task.WhenAll(a.HandleAsync(request, Ct), b.HandleAsync(request, Ct));
        Assert.Equal(1, gateway.Sends);
        var received = await queue.GetStateAsync(id, Ct);
        Assert.Equal(TaxDispatchStatus.Submitted, received.Status);
        Assert.Equal(gateway.Reference, received.ReferenceNumber);
        gateway.Result = new(gateway.Reference, "SUCCESS", """{"sign":"mock-receipt"}""");
        var result = await new InquireTaxSubmissionCommandHandler(repo, queue, gateway).HandleAsync(new(id), Ct);
        Assert.Equal(TaxDispatchStatus.Accepted, result.Status);
        var folder = await new InquireTaxWorkFolderCommandHandler(repo, gateway).HandleAsync(new(id), Ct);
        Assert.Equal("REJECTED", folder.InvoiceStatus);
        Assert.Equal(TaxDispatchStatus.Accepted, (await queue.GetStateAsync(id, Ct)).Status);
        var stored = await left.TaxDispatches.AsNoTracking().SingleAsync();
        Assert.StartsWith("test:", stored.ProtectedAcknowledgement);
        Assert.StartsWith("test:", stored.ProtectedProcessingEvidence);
        Assert.StartsWith("test:", stored.ProtectedWorkFolderEvidence);
    }

    [TaxationSqlFact]
    public async Task FailedReceiptWriteRecoversByInquiryWithoutRepeatingPost()
    {
        await using var db = Open();
        var id = await QueueSubmissionAsync(db);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER TaxTestRejectAcknowledgement ON TaxDispatches AFTER UPDATE AS
            BEGIN
                IF UPDATE(ProtectedAcknowledgement) THROW 50001, 'Mock receipt write failure', 1;
            END
            """);
        var gateway = new TestGateway();
        var queue = new TaxDispatchQueue(db, protection);
        var repository = new TaxSubmissionRepository(db, protection);
        var submission = new SubmitTaxInvoiceCommandHandler(repository, queue, new TestBindings(), gateway, TimeProvider.System);
        var command = new SubmitTaxInvoiceCommand(id, Filing());
        var unknown = await submission.HandleAsync(command, Ct);
        Assert.Equal(TaxDispatchStatus.Unknown, unknown.Status);
        Assert.NotNull(unknown.AttemptId);
        Assert.Null(unknown.ReferenceNumber);
        Assert.Equal(unknown, await submission.HandleAsync(command, Ct));

        await using var restarted = Open();
        var restoredQueue = new TaxDispatchQueue(restarted, protection);
        var inquiry = new InquireTaxSubmissionCommandHandler(new TaxSubmissionRepository(restarted, protection), restoredQueue, gateway);
        Assert.Equal(unknown, await inquiry.HandleAsync(new(id), Ct));
        Assert.Equal(unknown, await inquiry.HandleAsync(new(id), Ct));
        Assert.Equal(unknown.AttemptId, gateway.InquiredAttempt);
        gateway.Result = new(gateway.Reference, "IN_PROGRESS", """{"status":"IN_PROGRESS"}""");
        Assert.Equal(TaxDispatchStatus.Submitted, (await inquiry.HandleAsync(new(id), Ct)).Status);
        gateway.Result = new(gateway.Reference, "TIMEOUT", """{"status":"TIMEOUT"}""");
        Assert.Equal(TaxDispatchStatus.Submitted, (await inquiry.HandleAsync(new(id), Ct)).Status);
        gateway.Result = new(gateway.Reference, "SUCCESS", """{"status":"SUCCESS","sign":"mock"}""");
        var accepted = await inquiry.HandleAsync(new(id), Ct);
        Assert.Equal(TaxDispatchStatus.Accepted, accepted.Status);
        Assert.Equal(gateway.Reference, accepted.ReferenceNumber);
        gateway.Result = new(gateway.Reference, "FAILED", """{"status":"FAILED"}""");
        Assert.Equal(accepted, await inquiry.HandleAsync(new(id), Ct));
        Assert.Equal(1, gateway.Sends);
    }

    [TaxationSqlFact]
    public async Task ConcurrentCancellationCreatesOnlyOneIndependentSnapshotAndUsesExistingSubmit()
    {
        await using var db = Open();
        await using var other = Open();
        var reference = await SetupReferenceAsync(db, "AWAITING_REACTION");
        var command = new EnqueueTaxReferenceCommand(reference.Id, "tester",
            new(null, TaxDispatchKind.Cancellation, 2, Filing().IssuedAt, null, false, TaxCancellationReason.IncorrectTransactionDate));
        var left = ReferenceHandler(db);
        var right = ReferenceHandler(other);
        var ids = await Task.WhenAll(left.HandleAsync(command, Ct), right.HandleAsync(command, Ct));
        Assert.Equal(ids[0], ids[1]);
        Assert.Equal(2, await db.TaxInvoiceSnapshots.CountAsync());
        var original = await db.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == reference.Id);
        Assert.Equal(reference.ProtectedSubmission, original.ProtectedSubmission);
        Assert.Equal(TaxDispatchStatus.Accepted, original.Status);
        var canceled = await db.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == ids[0]);
        Assert.Equal(reference.Id, canceled.ReferencedDispatchId);
        Assert.Equal(reference.MemoryId, canceled.MemoryId);
        Assert.NotEqual(reference.SnapshotId, canceled.SnapshotId);
        var gateway = new TestGateway();
        var repository = new TaxSubmissionRepository(db, protection);
        var queue = new TaxDispatchQueue(db, protection);
        var submit = new SubmitTaxInvoiceCommandHandler(repository, queue, new TestBindings(), gateway, TimeProvider.System);
        Assert.Equal(TaxDispatchStatus.Submitted, (await submit.HandleAsync(new(canceled.Id, null), Ct)).Status);
        Assert.Equal(1, gateway.Sends);
        using var payload = JsonDocument.Parse(gateway.PreparedPayload);
        Assert.Equal(reference.TaxId, payload.RootElement.GetProperty("header").GetProperty("irtaxid").GetString());
        Assert.Equal(3, payload.RootElement.GetProperty("header").GetProperty("ins").GetInt32());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            left.HandleAsync(command with { ReferenceId = canceled.Id }, Ct));
    }

    [TaxationSqlFact]
    public async Task ReturnRejectsWrongParentAndFreezesRemainderWithReferenceMemory()
    {
        await using var db = Open();
        var reference = await SetupReferenceAsync(db, "APPROVED");
        var wrong = await AddCandidateAsync(db, reference.FactorId, 0.5m, Guid.NewGuid());
        var valid = await AddCandidateAsync(db, reference.FactorId, 0.5m, reference.FactorId);
        var operations = new TaxationRepository(db, protection);
        var replacement = await operations.SaveMemoryAsync(null, "XYZ123", "Other", Ct);
        await operations.ActivateAsync(replacement, Ct);
        var handler = ReferenceHandler(db);
        var request = new TaxReferenceRequest(wrong, TaxDispatchKind.Return, 2, Filing().IssuedAt, null, false, null);
        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(new(reference.Id, "tester", request), Ct));
        Assert.Single(await db.TaxInvoiceSnapshots.ToListAsync());
        var id = await handler.HandleAsync(new(reference.Id, "tester", request with { FactorId = valid }), Ct);
        var entry = await db.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(reference.MemoryId, entry.MemoryId);
        Assert.False(entry.MemoryWasActive);
        using var json = JsonDocument.Parse(protection.Unprotect(entry.ProtectedSubmission));
        Assert.Equal(0.5m, json.RootElement.GetProperty("body")[0].GetProperty("am").GetDecimal());
        Assert.Equal(55m, json.RootElement.GetProperty("header").GetProperty("tbill").GetDecimal());
        Assert.Equal(110m, await db.Factors.Where(x => x.Id == reference.FactorId).Select(x => x.PaymentAmount).SingleAsync());
        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(new(reference.Id, "tester",
            new(null, TaxDispatchKind.Cancellation, 3, Filing().IssuedAt, null, false, TaxCancellationReason.FullReturn)), Ct));
    }

    [TaxationSqlFact]
    public async Task CorrectionRequiresApprovalAndSerialIsUniqueInReferenceMemory()
    {
        await using var db = Open();
        var reference = await SetupReferenceAsync(db, "SYSTEMIC_APPROVED");
        var factor = await AddCandidateAsync(db, reference.FactorId, 2, null, FactorStatus.در_دست_تایید);
        var handler = ReferenceHandler(db);
        var request = new TaxReferenceRequest(factor, TaxDispatchKind.Correction, 2, Filing().IssuedAt, null, false, null);
        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(new(reference.Id, "tester", request), Ct));
        await db.Factors.Where(x => x.Id == factor).ExecuteUpdateAsync(s => s.SetProperty(x => x.status, FactorStatus.تایید_شده));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new(reference.Id, "tester", request with { Serial = 1 }), Ct));
        var id = await handler.HandleAsync(new(reference.Id, "tester", request), Ct);
        var corrected = await db.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal(TaxDispatchKind.Correction, corrected.Kind);
        Assert.Equal(reference.Id, corrected.ReferencedDispatchId);
        var operations = new TaxationRepository(db, protection);
        var version = await PreviewVersion(operations, factor);
        await Assert.ThrowsAsync<ArgumentException>(() => new EnqueueTaxInvoiceCommandHandler(operations)
            .HandleAsync(new(factor, "tester", version), Ct));

        var duplicateSerial = await Assert.ThrowsAsync<SqlException>(() => db.TaxDispatches.Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.InvoiceSerial, reference.InvoiceSerial)));
        Assert.Equal(2601, duplicateSerial.Number);

        await using var other = Open();
        var gateway = new TestGateway { WorkFolderStatus = "CANCELED" };
        var inquiry = new InquireTaxWorkFolderCommandHandler(new TaxSubmissionRepository(other, protection), gateway);
        gateway.BeforePrepare = () => inquiry.HandleAsync(new(reference.Id), Ct);
        var submissions = new TaxSubmissionRepository(db, protection);
        var submit = new SubmitTaxInvoiceCommandHandler(submissions, new TaxDispatchQueue(db, protection),
            new TestBindings(), gateway, TimeProvider.System);
        var blocked = await submit.HandleAsync(new(id, null), Ct);
        Assert.Equal(TaxDispatchStatus.Pending, blocked.Status);
        Assert.Null(blocked.AttemptId);
        await Assert.ThrowsAsync<ArgumentException>(() => submit.HandleAsync(new(id, null), Ct));
        Assert.Equal(0, gateway.Sends);
    }

    [TaxationSqlFact]
    public async Task ExportFreezesCustomsDataAndUsesExistingQueueAndMockInquiry()
    {
        await using var db = Open();
        await using var other = Open();
        var (factorId, memoryId) = await SeedAsync(db, FactorStatus.تایید_شده);
        var filing = Filing() with { BuyerType = 0 };
        var details = await SetExportFixtureAsync(db, factorId, filing.IssuedAt.AddDays(-1));
        var operations = new TaxationRepository(db, protection);
        var preview = await new PreviewTaxInvoiceQueryHandler(operations).HandleAsync(new(factorId, details), Ct);
        Assert.Empty(preview.Errors);
        var enqueue = new EnqueueTaxInvoiceCommandHandler(operations);
        var changed = details with { Lines = [details.Lines[0] with { ExchangeRate = 90001 }] };
        await Assert.ThrowsAsync<TaxPreviewConflictException>(() =>
            enqueue.HandleAsync(new(factorId, "tester", preview.Version, changed), Ct));
        Assert.Empty(await db.TaxInvoiceSnapshots.ToListAsync());
        var request = new EnqueueTaxInvoiceCommand(factorId, "tester", preview.Version, details);
        var ids = await Task.WhenAll(enqueue.HandleAsync(request, Ct),
            new EnqueueTaxInvoiceCommandHandler(new TaxationRepository(other, protection)).HandleAsync(request, Ct));
        Assert.Equal(ids[0], ids[1]);
        var queued = await db.TaxDispatches.AsNoTracking().SingleAsync();
        var saved = JsonSerializer.Deserialize<InvoicePreview>(await operations.ReadSnapshotAsync(queued.SnapshotId, Ct));
        Assert.Equal(memoryId, queued.MemoryId);
        Assert.Equal(TaxInvoicePattern.Export, saved.Pattern);
        Assert.Equal(1, saved.InvoiceType);
        Assert.Equal(100m, saved.Total);
        Assert.Equal(150000m, saved.ExportDetails.Lines[0].RialValue);

        await other.Factors.Where(x => x.Id == factorId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaymentAmount, 99m));
        var repository = new TaxSubmissionRepository(db, protection);
        var queue = new TaxDispatchQueue(db, protection);
        var gateway = new TestGateway { WorkFolderStatus = "APPROVED" };
        var submit = new SubmitTaxInvoiceCommandHandler(repository, queue, new TestBindings(), gateway, TimeProvider.System);
        var state = await submit.HandleAsync(new(queued.Id, filing), Ct);
        Assert.Equal(TaxDispatchStatus.Submitted, state.Status);
        Assert.True(TaxInvoicePayload.MatchesTaxId(queued.FiscalId, state.TaxId, filing.Serial));
        using var posted = JsonDocument.Parse(gateway.PreparedPayload);
        Assert.Equal(150000m, posted.RootElement.GetProperty("header").GetProperty("tbill").GetDecimal());
        Assert.Equal(1, gateway.Sends);
        Assert.Equal(state, await submit.HandleAsync(new(queued.Id, filing), Ct));
        Assert.Equal(1, gateway.Sends);
        gateway.Result = new(gateway.Reference, "SUCCESS", """{"sign":"mock"}""");
        Assert.Equal(TaxDispatchStatus.Accepted,
            (await new InquireTaxSubmissionCommandHandler(repository, queue, gateway).HandleAsync(new(queued.Id), Ct)).Status);
        await new InquireTaxWorkFolderCommandHandler(repository, gateway).HandleAsync(new(queued.Id), Ct);

        var correctionId = await AddCandidateAsync(db, factorId, 2, null);
        var correctionDetails = await SetExportFixtureAsync(db, correctionId, filing.IssuedAt.AddDays(-1));
        var reference = ReferenceHandler(db);
        var correction = new TaxReferenceRequest(correctionId, TaxDispatchKind.Correction, 2, filing.IssuedAt,
            null, false, null, correctionDetails with { CustomsDeclarationNumber = "2000" });
        var correctedId = await reference.HandleAsync(new(queued.Id, "tester", correction), Ct);
        var corrected = await db.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == correctedId);
        Assert.Equal(queued.Id, corrected.ReferencedDispatchId);
        Assert.NotEqual(state.TaxId, corrected.TaxId);
        using var correctedPayload = JsonDocument.Parse(protection.Unprotect(corrected.ProtectedSubmission));
        Assert.Equal(7, correctedPayload.RootElement.GetProperty("header").GetProperty("inp").GetInt32());
        Assert.Equal(state.TaxId, correctedPayload.RootElement.GetProperty("header").GetProperty("irtaxid").GetString());
        Assert.Equal(2, await db.TaxInvoiceSnapshots.CountAsync());
    }

    private static async Task<ExportTaxDetails> SetExportFixtureAsync(CRMContextDb db, Guid factor, DateTimeOffset customsDate)
    {
        await db.Factors.Where(x => x.Id == factor).ExecuteUpdateAsync(s => s.SetProperty(x => x.Export, true)
            .SetProperty(x => x.CurrencyType, CurrencyTypeEnum.یورو).SetProperty(x => x.PercentTax, 0)
            .SetProperty(x => x.TotalTax, 0).SetProperty(x => x.PaymentAmount, x => x.SumTotalAmount));
        await db.FactorItems.Where(x => x.factorId == factor).ExecuteUpdateAsync(s => s.SetProperty(x => x.TaxAmount, 0)
            .SetProperty(x => x.TotalAmount, x => x.TotalAmount - x.TaxAmount));
        var item = await db.FactorItems.Where(x => x.factorId == factor).Select(x => x.Id).SingleAsync();
        return new("1000", customsDate, "12345", true, [new ExportTaxLine(item, "EUR", 90000, 150000, 1.5m, 10)]);
    }

    private EnqueueTaxReferenceCommandHandler ReferenceHandler(CRMContextDb db, ITaxPreviewProtector protector = null) =>
        new(new TaxationRepository(db, protector ?? protection), new TestBindings(), TimeProvider.System);

    private async Task<TaxDispatch> SetupReferenceAsync(CRMContextDb db, string workFolder, ITaxPreviewProtector protector = null)
    {
        protector ??= protection;
        var id = await QueueSubmissionAsync(db, protector);
        var gateway = new TestGateway { WorkFolderStatus = workFolder };
        var queue = new TaxDispatchQueue(db, protector);
        var repository = new TaxSubmissionRepository(db, protector);
        var submit = new SubmitTaxInvoiceCommandHandler(repository, queue, new TestBindings(), gateway, TimeProvider.System);
        await submit.HandleAsync(new(id, Filing()), Ct);
        gateway.Result = new(gateway.Reference, "SUCCESS", """{"sign":"mock"}""");
        await new InquireTaxSubmissionCommandHandler(repository, queue, gateway).HandleAsync(new(id), Ct);
        await new InquireTaxWorkFolderCommandHandler(repository, gateway).HandleAsync(new(id), Ct);
        return await db.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == id);
    }

    private static async Task<Guid> AddCandidateAsync(CRMContextDb db, Guid originalId, decimal quantity, Guid? parent,
        FactorStatus status = FactorStatus.تایید_شده)
    {
        var reference = await db.Factors.AsNoTracking().Include(x => x.FactorItems).SingleAsync(x => x.Id == originalId);
        var gross = quantity * 100;
        var vat = gross / 10;
        var factor = new Factor
        {
            Id = Guid.NewGuid(), PfactorID = reference.PfactorID, DateMiladi = reference.DateMiladi,
            CustomerId = reference.CustomerId, Cus_EghtesadiCode = reference.Cus_EghtesadiCode,
            Cus_ShenaseMeli = reference.Cus_ShenaseMeli, OfficialType = 1, status = status, PercentTax = 10,
            ParentReturnedId = parent, SumTotalAmount = gross, TotalTax = vat, PaymentAmount = gross + vat,
            FactorItems = [new FactorItem
            {
                Id = Guid.NewGuid(), Row = 1, ProductCode = reference.FactorItems.Single().ProductCode,
                WeightCount = quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                UnitAmount = 100, TaxAmount = vat, TotalAmount = gross + vat
            }]
        };
        db.Factors.Add(factor);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return factor.Id;
    }

    [TaxationSqlFact]
    public async Task MixedSettlementIsPinnedBeforeSubmitAndReusedAfterLocalFailure()
    {
        await using var db = Open();
        var id = await QueueSubmissionAsync(db);
        var gateway = new TestGateway { FailPrepare = true };
        var repository = new TaxSubmissionRepository(db, protection);
        var queue = new TaxDispatchQueue(db, protection);
        var submit = new SubmitTaxInvoiceCommandHandler(repository, queue, new TestBindings(), gateway, TimeProvider.System);
        var mixed = Filing() with { Settlement = 3, Shares = new(60, 40) };
        await Assert.ThrowsAsync<CryptographicException>(() => submit.HandleAsync(new(id, mixed), Ct));
        var pinned = await repository.FindAsync(id, Ct);
        Assert.Equal(TaxDispatchStatus.Pending, pinned.Status);
        using var first = JsonDocument.Parse(pinned.Payload);
        Assert.Equal(60m, first.RootElement.GetProperty("header").GetProperty("cap").GetDecimal());

        gateway.FailPrepare = false;
        var state = await submit.HandleAsync(new(id, mixed with { Shares = new(70, 30) }), Ct);
        Assert.Equal(TaxDispatchStatus.Submitted, state.Status);
        using var submitted = JsonDocument.Parse(gateway.PreparedPayload);
        var header = submitted.RootElement.GetProperty("header");
        Assert.Equal(60m, header.GetProperty("cap").GetDecimal());
        Assert.Equal(40m, header.GetProperty("insp").GetDecimal());
        Assert.Equal(1, gateway.Sends);
    }

    [TaxationSqlFact]
    public async Task ApprovedPaymentIsFrozenOnceAndAmbiguousPaymentIsNeverRepeated()
    {
        await using var db = Open();
        await using var other = Open();
        var invoice = await SetupReferenceAsync(db, "APPROVED");
        var paidAt = Filing().IssuedAt.AddMinutes(1);
        var receipt = new Accountancy
        {
            Id = Guid.NewGuid(), fid = invoice.FactorId, InvoiceType = InvoiceTypeEnum.فاکتور,
            AccType = AcEnum.واریزی, Status = AccStatusEnum.تایید_شده, PayType = PayTypeEnum.چکی,
            CurrencyType = CurrencyTypeEnum.ریال, Vosoled = true, VosolDateM = paidAt.Date, credit = 40
        };
        db.Accountancies.Add(receipt);
        await db.SaveChangesAsync();
        var payments = new TaxPaymentRepository(db, protection);
        var enqueue = new EnqueueTaxPaymentCommandHandler(payments, TimeProvider.System);
        var command = new EnqueueTaxPaymentCommand(invoice.Id, "tester", new(receipt.Id, TaxReceiptMethod.Cheque, paidAt, "ref-001", null));
        var ids = await Task.WhenAll(enqueue.HandleAsync(command, Ct),
            new EnqueueTaxPaymentCommandHandler(new TaxPaymentRepository(other, protection), TimeProvider.System)
                .HandleAsync(command, Ct));
        Assert.Equal(ids[0], ids[1]);
        Assert.Single(await db.TaxPaymentDispatches.AsNoTracking().ToListAsync());
        var pending = await payments.ReadPendingAsync(ids[0], Ct);
        using var posted = JsonDocument.Parse(pending.Payload);
        Assert.Equal(40, posted.RootElement.GetProperty("paidAmount").GetInt32());
        Assert.Equal(paidAt.ToUnixTimeMilliseconds(), posted.RootElement.GetProperty("paymentDate").GetInt64());

        var mock = new MockPaymentTransport { Timeout = true };
        var submit = new SubmitTaxPaymentCommandHandler(payments, new TaxSubmissionRepository(db, protection), mock, TimeProvider.System);
        var submitOther = new SubmitTaxPaymentCommandHandler(new TaxPaymentRepository(other, protection),
            new TaxSubmissionRepository(other, protection), mock, TimeProvider.System);
        await Task.WhenAll(submit.HandleAsync(new(ids[0]), Ct), submitOther.HandleAsync(new(ids[0]), Ct));
        var state = await payments.GetStateAsync(ids[0], Ct);
        Assert.Equal(TaxPaymentStatus.Unknown, state.Status);
        Assert.NotNull(state.AttemptId);
        Assert.Equal(state, await submitOther.HandleAsync(new(ids[0]), Ct));
        Assert.Equal(1, mock.Calls);
    }

    [TaxationSqlFact]
    public async Task DefinitivePaymentResponseIsProtectedAndReadableAsLocalStatus()
    {
        await using var db = Open();
        var invoice = await SetupReferenceAsync(db, "APPROVED");
        var paidAt = Filing().IssuedAt.AddMinutes(1);
        var receipt = new Accountancy
        {
            Id = Guid.NewGuid(), fid = invoice.FactorId, InvoiceType = InvoiceTypeEnum.فاکتور,
            AccType = AcEnum.واریزی, Status = AccStatusEnum.تایید_شده, PayType = PayTypeEnum.نقدی,
            CurrencyType = CurrencyTypeEnum.ریال, date = paidAt.Date, credit = 40
        };
        db.Accountancies.Add(receipt);
        await db.SaveChangesAsync();
        var payments = new TaxPaymentRepository(db, protection);
        var id = await new EnqueueTaxPaymentCommandHandler(payments, TimeProvider.System).HandleAsync(
            new(invoice.Id, "tester", new(receipt.Id, TaxReceiptMethod.Transfer, paidAt, "ref-002", null)), Ct);
        var mock = new MockPaymentTransport();
        var state = await new SubmitTaxPaymentCommandHandler(payments, new TaxSubmissionRepository(db, protection),
            mock, TimeProvider.System).HandleAsync(new(id), Ct);
        Assert.Equal(TaxPaymentStatus.Accepted, state.Status);
        Assert.NotNull(state.ResolvedAt);
        var stored = await db.TaxPaymentDispatches.AsNoTracking().SingleAsync();
        Assert.StartsWith("test:", stored.ProtectedResponse);
        Assert.Equal(state, await payments.GetStateAsync(id, Ct));
    }

    [TaxationSqlFact]
    public async Task PaymentMigrationPreservesAcceptedInvoiceSnapshotAndRelatedDispatch()
    {
        var keyRing = Directory.CreateTempSubdirectory("vazin-tax-test-keys-");
        try
        {
            await VerifyExistingDispatchesSurvivePaymentMigrationAsync(keyRing);
        }
        finally
        {
            keyRing.Delete(recursive: true);
        }
    }

    private async Task VerifyExistingDispatchesSurvivePaymentMigrationAsync(DirectoryInfo keyRing)
    {
        Guid originalId;
        Guid relatedId;
        string originalSnapshot;
        TaxDispatch original;
        await using (var before = Open())
        {
            var preceding = before.Database.GetMigrations()
                .TakeWhile(x => !x.EndsWith("_AddTaxPaymentDispatches", StringComparison.Ordinal)).Last();
            await before.GetService<IMigrator>().MigrateAsync(preceding, Ct);

            var protector = new TaxPreviewProtector(DataProtectionProvider.Create(keyRing));
            original = await SetupReferenceAsync(before, "AWAITING_REACTION", protector);
            originalId = original.Id;
            var taxation = new TaxationRepository(before, protector);
            originalSnapshot = await taxation.ReadSnapshotAsync(original.SnapshotId, Ct);
            relatedId = await ReferenceHandler(before, protector).HandleAsync(new(originalId, "tester",
                new(null, TaxDispatchKind.Cancellation, 2, Filing().IssuedAt.AddMinutes(2), null, false,
                    TaxCancellationReason.IncorrectTransactionDate)), Ct);
            await before.Database.MigrateAsync(Ct);
        }

        await using var after = Open();
        Assert.Empty(await after.Database.GetPendingMigrationsAsync(Ct));
        var reloaded = await after.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == originalId, Ct);
        Assert.Equal(TaxDispatchStatus.Accepted, reloaded.Status);
        Assert.Equal(original.TaxId, reloaded.TaxId);
        Assert.Equal(original.AuthorityReference, reloaded.AuthorityReference);
        Assert.Equal(original.ProtectedSubmission, reloaded.ProtectedSubmission);
        Assert.Equal(original.ProtectedAcknowledgement, reloaded.ProtectedAcknowledgement);
        Assert.Equal(original.ProtectedProcessingEvidence, reloaded.ProtectedProcessingEvidence);
        Assert.Equal(original.ProtectedWorkFolderEvidence, reloaded.ProtectedWorkFolderEvidence);
        var restartedProtector = new TaxPreviewProtector(DataProtectionProvider.Create(keyRing));
        var taxationAfter = new TaxationRepository(after, restartedProtector);
        Assert.Equal(originalSnapshot, await taxationAfter.ReadSnapshotAsync(reloaded.SnapshotId, Ct));
        var related = await after.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == relatedId, Ct);
        Assert.Equal(TaxDispatchKind.Cancellation, related.Kind);
        Assert.Equal(originalId, related.ReferencedDispatchId);
        Assert.Equal(reloaded.MemoryId, related.MemoryId);
        Assert.NotEqual(reloaded.SnapshotId, related.SnapshotId);
        Assert.Empty(await after.TaxPaymentDispatches.ToListAsync(Ct));
    }

    private sealed class MockPaymentTransport : ITaxPaymentTransport
    {
        private int calls;
        public int Calls => calls;
        public bool Timeout { get; init; }

        public Task<PreparedTaxRequest> PreparePaymentAsync(TaxSubmission invoice, Guid attemptId, string payload, CancellationToken ct) =>
            Task.FromResult(new PreparedTaxRequest(attemptId, new HttpRequestMessage()));

        public Task<TaxPaymentReply> SubmitPaymentAsync(PreparedTaxRequest request, CancellationToken ct)
        {
            Interlocked.Increment(ref calls);
            if (Timeout) throw new TaskCanceledException();
            return Task.FromResult(new TaxPaymentReply(TaxPaymentStatus.Accepted, """{"requestStatus":"SUCCESS"}"""));
        }
    }

    private async Task<Guid> QueueSubmissionAsync(CRMContextDb db, ITaxPreviewProtector protector = null)
    {
        var (factor, _) = await SeedAsync(db, FactorStatus.تایید_شده);
        var repo = new TaxationRepository(db, protector ?? protection);
        return await new EnqueueTaxInvoiceCommandHandler(repo)
            .HandleAsync(new(factor, "tester", await PreviewVersion(repo, factor)), Ct);
    }

    private static TaxFilingDetails Filing() => new(1, new DateTimeOffset(
        DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified), TimeSpan.FromMinutes(210)), 2, 2);

    private sealed class TestBindings : ITaxSigningBindings
    {
        public string Hash { get; set; } = new('A', 64);
        public TaxSigningBinding Get(Guid memoryId) => new(TaxAuthorityEnvironment.Sandbox, Hash, "14003778990");
    }

    private sealed class TestGateway : ITaxAuthorityClient
    {
        private int sends;
        public int Sends => sends;
        public bool FailPrepare { get; set; }
        public bool Timeout { get; set; }
        public Guid Reference { get; } = Guid.NewGuid();
        public Guid? InquiredAttempt { get; private set; }
        public string PreparedCertificate { get; private set; }
        public string PreparedPayload { get; private set; }
        public string WorkFolderStatus { get; init; } = "REJECTED";
        public Func<Task> BeforePrepare { get; set; }
        public TaxProcessingResult Result { get; set; }

        public async Task<PreparedTaxRequest> PrepareAsync(TaxSubmission submission, Guid attemptId, CancellationToken ct)
        {
            if (FailPrepare) throw new CryptographicException();
            PreparedCertificate = submission.CertificateSha256;
            PreparedPayload = submission.Payload;
            if (BeforePrepare != null)
                await BeforePrepare();
            return new PreparedTaxRequest(attemptId, new HttpRequestMessage());
        }

        public Task<TaxAcknowledgement> SendAsync(PreparedTaxRequest request, CancellationToken ct)
        {
            Interlocked.Increment(ref sends);
            if (Timeout) throw new TaskCanceledException();
            return Task.FromResult(new TaxAcknowledgement(Reference, """{"receipt":"mock"}"""));
        }

        public Task<TaxProcessingResult> InquireAsync(TaxSubmission submission, CancellationToken ct)
        {
            InquiredAttempt = submission.AttemptId;
            return Task.FromResult(Result);
        }

        public Task<TaxWorkFolderResult> GetWorkFolderAsync(TaxSubmission submission, CancellationToken ct) =>
            Task.FromResult(new TaxWorkFolderResult(WorkFolderStatus, "NOT_EXCEEDED", null,
                JsonSerializer.Serialize(new { taxId = submission.TaxId, invoiceStatus = WorkFolderStatus })));
    }

    private sealed class UnreadableProtection : ITaxPreviewProtector
    {
        public string Protect(string preview) => throw new NotSupportedException();
        public string Unprotect(string preview) => throw new CryptographicException();
    }

    private static async Task RunLocalDbAsync(string action)
    {
        var startInfo = new ProcessStartInfo("SqlLocalDB.exe") { UseShellExecute = false, CreateNoWindow = true };
        startInfo.ArgumentList.Add(action);
        startInfo.ArgumentList.Add("VazinTaxationStage2Tests");
        using var process = Process.Start(startInfo);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        await process.WaitForExitAsync(timeout.Token);
        Assert.Equal(0, process.ExitCode);
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
            Cus_EghtesadiCode = "10100302746", Cus_ShenaseMeli = "10100302746", OfficialType = 1, status = status, PercentTax = 10,
            SumTotalAmount = 100, Discount = 0, TotalTax = 10, PaymentAmount = 110,
            FactorItems = [new FactorItem { Id = Guid.NewGuid(), Row = 1, ProductCode = product.Id,
                WeightCount = "1", UnitAmount = 100, Discount = 0, TaxAmount = 10, TotalAmount = 110 }]
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

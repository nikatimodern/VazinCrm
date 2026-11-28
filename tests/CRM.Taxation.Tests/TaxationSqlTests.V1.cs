using CRM.Core.Domain.Taxation;
using CRM.Infrastructures.Sql.Taxation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed partial class TaxationSqlTests
{
    [TaxationSqlFact]
    public async Task V1MigrationPreservesV2DispatchAndPinsV1Fingerprint()
    {
        Guid previousV2;
        var fingerprint = new string('A', 64);
        await using (var db = Open())
        {
            previousV2 = await QueueSubmissionAsync(db);
            var snapshot = await db.TaxDispatches.Where(x => x.Id == previousV2).Select(x => x.SnapshotId).SingleAsync(Ct);
            var previousMigration = db.Database.GetMigrations().Reverse()
                .First(x => !x.EndsWith("_PinTaxAuthorityProtocol", StringComparison.Ordinal));
            await db.GetService<IMigrator>().MigrateAsync(previousMigration, Ct);
            await db.Database.MigrateAsync(Ct);
            var migrated = await db.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == previousV2, Ct);
            Assert.Equal(snapshot, migrated.SnapshotId);
            Assert.Equal(TaxAuthorityProtocol.V2, migrated.AuthorityProtocol);
            Assert.Null(migrated.V1KeyFingerprint);

            var repo = new TaxSubmissionRepository(db, protection);
            Assert.False(await repo.HasOpenSubmissionsAsync(TaxAuthorityProtocol.V2, Ct));
            await repo.PinAsync(previousV2, new(TaxAuthorityEnvironment.Sandbox, null, "12345678901234",
                TaxAuthorityProtocol.V1, fingerprint), 42, "ABC1231234567890123456", "{}", Ct);
            Assert.True(await repo.HasOpenSubmissionsAsync(TaxAuthorityProtocol.V1, Ct));
            Assert.False(await repo.HasOpenSubmissionsAsync(TaxAuthorityProtocol.V2, Ct));
        }
        await using var restarted = Open();
        var pinned = await new TaxSubmissionRepository(restarted, protection).FindAsync(previousV2, Ct);
        Assert.Equal(TaxAuthorityProtocol.V1, pinned.Protocol);
        Assert.Null(pinned.CertificateSha256);
        Assert.Equal(fingerprint, pinned.V1KeyFingerprint);
        Assert.Equal("{}", pinned.Payload);
    }
}

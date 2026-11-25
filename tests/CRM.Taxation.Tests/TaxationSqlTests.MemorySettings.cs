using System.Net;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxConnectionCheck;
using CRM.Core.Domain.Taxation;
using CRM.Infrastructures.Sql.Taxation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed partial class TaxationSqlTests
{
    [TaxationSqlFact]
    public async Task ManagedTaxMemorySettingsAuditRotationAndKeepPinnedCredentials()
    {
        await using var db = Open();
        var dispatchId = await QueueSubmissionAsync(db);
        var repo = new TaxationRepository(db, protection);
        var memory = await db.TaxMemories.AsNoTracking().SingleAsync(x => x.IsActive, Ct);
        Assert.Equal(TaxAuthorityEnvironment.Sandbox, memory.Environment);
        Assert.Equal(TaxAuthorityProtocol.V2, memory.Protocol);

        var oldKey = new string('A', 64);
        var replacement = new string('B', 64);
        var first = new TaxMemorySettings(memory.Id, "Sandbox memory", memory.FiscalId, TaxAuthorityEnvironment.Sandbox,
            TaxAuthorityProtocol.V2, "12345678901234", oldKey, false, memory.UpdatedUtc);
        await repo.SaveSettingsAsync(first, "admin-test", Ct);
        db.ChangeTracker.Clear();
        var setup = await repo.FindMemoryAsync(memory.Id, Ct);
        var submissions = new TaxSubmissionRepository(db, protection);
        await submissions.PinAsync(dispatchId, new(TaxAuthorityEnvironment.Sandbox, oldKey, first.SellerEconomicCode),
            42, "ABC1231234567890123456", "{}", Ct);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.SaveSettingsAsync(first with
        {
            Protocol = TaxAuthorityProtocol.V1, ExpectedUpdatedUtc = setup.UpdatedUtc
        }, "admin-test", Ct));
        db.ChangeTracker.Clear();
        await repo.SaveSettingsAsync(first with { CredentialFingerprint = replacement, ExpectedUpdatedUtc = setup.UpdatedUtc },
            "admin-test", Ct);
        Assert.Equal(oldKey, (await submissions.FindAsync(dispatchId, Ct)).CertificateSha256);
        Assert.Equal(replacement, (await repo.FindMemoryAsync(memory.Id, Ct)).CredentialFingerprint);

        var productionId = await repo.SaveSettingsAsync(first with
        {
            Id = null, Environment = TaxAuthorityEnvironment.Production, Protocol = TaxAuthorityProtocol.V1,
            ExpectedUpdatedUtc = null
        }, "admin-test", Ct);
        await Assert.ThrowsAsync<ArgumentException>(() => repo.ActivateAsync(productionId, Ct));
        Assert.Equal(memory.Id, (await repo.FindActiveMemoryAsync(Ct)).Id);
        Assert.Equal(3, await db.TaxMemoryAuditEvents.CountAsync(x => x.ActorId == "admin-test", Ct));
        Assert.Contains(await db.TaxMemoryAuditEvents.ToListAsync(Ct), x => x.Operation == "SettingsAndCredentialChanged");

        var keyRing = Directory.CreateTempSubdirectory("vazin-tax-settings-http-");
        try
        {
            using var authority = new TaxationMockAuthority();
            await using var app = await TaxationHttpTestHost.StartAsync(options, keyRing, authority, Ct);
            using var browser = TaxationHttpTestHost.Connect(app);
            using var sales = TaxationHttpTestHost.Connect(app, "sales");
            using var forbidden = await sales.GetAsync("/Taxation/memories", Ct);
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            var page = await GetPageAsync(browser, $"/Taxation/memories?saved={memory.Id:D}");
            Assert.Contains("تنظیمات حافظه ذخیره شد.", page);
            Assert.Contains("جفت کلید ثبت شده", page);
            Assert.Contains("گواهی ثبت شده", page);
            Assert.Contains("انتخاب فایل کلید خصوصی PEM", page);
            Assert.Contains("انتخاب فایل گواهی PFX یا P12", page);
            Assert.Contains("تست اتصال آزمایشی", page);
            Assert.Contains("بررسی تنظیمات و کلید یا گواهی", page);
            var result = await PostAsync<TaxConnectionStep[]>(browser, $"/api/taxation/memories/{productionId:D}/check-connection", null);
            Assert.Equal("Passed", result[0].Status);
            Assert.Equal("Skipped", result[2].Status);
            Assert.Equal(0, authority.InvoicePosts);
        }
        finally { keyRing.Delete(recursive: true); }
    }
}

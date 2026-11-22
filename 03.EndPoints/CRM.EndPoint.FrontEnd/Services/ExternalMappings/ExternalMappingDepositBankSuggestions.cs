using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Sepidar.Deposits.Entities;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings;

public sealed partial class ExternalMappingService
{
    private sealed record DepositMatchSource(Guid Id, Guid CustomerId, Guid? BankId,
        PayTypeEnum Type, decimal Amount, DateTime Date, DateTime? Due, bool Collected, bool Transferred);

    private sealed record ReceiptMatch(int Id, int Dl, int ItemType, DateTime Date, decimal Amount);
    private sealed record NoteMatch(int Id, DateTime Date, decimal Amount);

    private static string NormalizeBankIdentifier(string value) =>
        new string(TextService.ToEnglishNumber(value ?? "").ToUpperInvariant()
            .Where(c => char.IsLetterOrDigit(c)).ToArray());

    private static string NormalizeBankName(string value)
    {
        var normalized = new string(TextService.NormalizePersianText(value ?? "")
            .Where(char.IsLetterOrDigit).ToArray());
        return normalized.StartsWith("بانک", StringComparison.Ordinal) ? normalized[4..] : normalized;
    }

    private async Task<List<MappingReportEntry>> BuildDepositAndBankSuggestionsAsync(
        Guid softwareId, SepidarContextDbBase context, CancellationToken ct)
    {
        var result = new List<MappingReportEntry>();
        var currency = await MappingRialCurrencyAsync(context, ct);
        var deposits = await EligibleDeposits(db.Accountancies.AsNoTracking())
            .Select(x => new DepositMatchSource(x.Id, x.CustomerId.Value, x.BankAccountId, x.PayType,
                x.credit.Value, (x.PayType == PayTypeEnum.چکی ? x.ReciveDateM : x.VosolDateM ?? x.date) ?? x.CreateDate,
                x.VosolDateM, x.Vosoled, x.AccountancyTransfer != null && !x.AccountancyTransfer.IsDeleted))
            .ToListAsync(ct);
        var mappings = await db.ExternalMappings.AsNoTracking().Where(x => x.ExternalSoftwareId == softwareId
            && (x.MapType == ExternalMappingTypeEnum.واریزی || x.MapType == ExternalMappingTypeEnum.حساب_بانکی
                || x.MapType == ExternalMappingTypeEnum.مشتری)).ToListAsync(ct);
        var depositMaps = mappings.Where(x => x.MapType == ExternalMappingTypeEnum.واریزی && x.EntityName == nameof(Accountancy))
            .ToLookup(x => x.EntityId);
        var bankMaps = mappings.Where(x => x.MapType == ExternalMappingTypeEnum.حساب_بانکی && x.EntityName == nameof(BankAccount))
            .ToLookup(x => x.EntityId);
        var externalBanks = await context.SepidarBankAccounts.AsNoTracking().Where(x => x.CurrencyRef == currency)
            .Select(x => new { x.BankAccountId, x.AccountNo, x.ShebaNumber, x.CreditCardNumber,
                Bank = x.SepidarBankBranch.SepidarBank.Title }).ToListAsync(ct);
        var validBankIds = externalBanks.Select(x => x.BankAccountId).ToHashSet();
        var bankOwners = mappings.Where(x => x.MapType == ExternalMappingTypeEnum.حساب_بانکی && x.ExEntityName == "BankAccount")
            .ToLookup(x => x.ExEntityId, x => x.EntityId);
        var usedBankIds = deposits.Where(x => x.BankId.HasValue).Select(x => x.BankId.Value).ToHashSet();
        // All internal company accounts, plus accounts actually referenced by the deposit workflow.
        var crmBankRows = await db.BankAccounts.AsNoTracking().Where(x => !x.IsDeleted && x.Active)
            .Select(x => new { x.Id, x.BankAccountType, x.AccountNumber, x.ShebaNumber, x.CartNumber, x.BankName }).ToListAsync(ct);
        var crmBanks = crmBankRows.Where(x => x.BankAccountType == BankAccountTypeEnum.شرکت || usedBankIds.Contains(x.Id)).ToList();

        // Existing cash receipt mappings provide explicit bank evidence, without guessing
        // from a receipt header's DL (that DL belongs to the customer, not the bank).
        var linkedCash = deposits.Where(x => x.Type == PayTypeEnum.نقدی && x.BankId.HasValue && depositMaps[x.Id].Count() == 1)
            .Select(x => new { Source = x, Map = depositMaps[x.Id].Single() })
            .Where(x => x.Map.ExEntityName == nameof(ReceiptHeader) && int.TryParse(x.Map.ExEntityId, out _))
            .ToList();
        var linkedIds = linkedCash.Select(x => int.Parse(x.Map.ExEntityId)).Where(x => x > 0).Distinct().ToArray();
        var bankEvidence = new Dictionary<int, List<int>>();
        foreach (var batch in linkedIds.Chunk(350))
        {
            var rows = await (from draft in context.ReceiptDrafts.AsNoTracking()
                join receipt in context.ReceiptHeaders.AsNoTracking() on draft.ReceiptHeaderRef equals receipt.ReceiptHeaderId
                where batch.Contains(receipt.ReceiptHeaderId) && receipt.Type == 1 && receipt.ItemType == 8
                    && receipt.CurrencyRef == currency && draft.BankAccountRef.HasValue
                select new { receipt.ReceiptHeaderId, BankId = draft.BankAccountRef.Value }).ToListAsync(ct);
            foreach (var group in rows.GroupBy(x => x.ReceiptHeaderId))
                bankEvidence[group.Key] = group.Select(x => x.BankId).Distinct().ToList();
        }
        var evidenceByBank = linkedCash.Where(x => bankEvidence.ContainsKey(int.Parse(x.Map.ExEntityId)))
            .ToLookup(x => x.Source.BankId.Value, x => (ReceiptId: int.Parse(x.Map.ExEntityId), x.Source.Id));
        var bankCandidates = new Dictionary<Guid, int[]>();
        var bankNotes = new Dictionary<Guid, string[]>();
        foreach (var bank in crmBanks)
        {
            var sheba = NormalizeBankIdentifier(bank.ShebaNumber);
            var card = NormalizeBankIdentifier(bank.CartNumber);
            var account = NormalizeBankIdentifier(bank.AccountNumber);
            var bankName = NormalizeBankName(bank.BankName);
            var matches = externalBanks.Where(x =>
                (sheba.Length == 26 && sheba.StartsWith("IR") && sheba == NormalizeBankIdentifier(x.ShebaNumber))
                || (card.Length == 16 && card == NormalizeBankIdentifier(x.CreditCardNumber))
                || (account.Length > 0 && bankName.Length > 0 && account == NormalizeBankIdentifier(x.AccountNo)
                    && bankName == NormalizeBankName(x.Bank))).Select(x => x.BankAccountId);
            var evidence = evidenceByBank[bank.Id].ToArray();
            bankCandidates[bank.Id] = matches.Concat(evidence.SelectMany(x => bankEvidence[x.ReceiptId]))
                .Where(validBankIds.Contains).Distinct().OrderBy(x => x).ToArray();
            bankNotes[bank.Id] = evidence.Select(x => $"حساب مقصد از حواله رسید دریافت سپیدار با شناسه {x.ReceiptId} به‌دست آمده است.")
                .Distinct().ToArray();
        }
        var contestedBanks = bankCandidates.SelectMany(x => x.Value.Select(id => (Source: x.Key, Target: id)))
            .Where(x => !bankMaps[x.Source].Any()).GroupBy(x => x.Target)
            .Where(g => g.Select(x => x.Source).Distinct().Count() > 1).Select(g => g.Key).ToHashSet();
        foreach (var (sourceId, candidates) in bankCandidates)
        {
            var saved = bankMaps[sourceId].ToArray();
            if (saved.Length > 0)
            {
                if (candidates.Length > 0 && (saved.Length != 1 || saved[0].ExEntityName != "BankAccount"
                    || !int.TryParse(saved[0].ExEntityId, out var savedId) || candidates.Any(x => x != savedId)))
                    result.Add(FinancialProposal(sourceId, ExternalMappingTypeEnum.حساب_بانکی, nameof(BankAccount),
                        "BankAccount", "blocked", "BankEvidenceConflict", null, candidates, bankNotes[sourceId]));
                continue;
            }
            var status = candidates.Length == 0 ? "notMatched" : candidates.Length > 1 ? "ambiguous" : "pending";
            var reason = candidates.Length == 0 ? "BankNoMatch" : candidates.Length > 1 ? "BankAmbiguous" : "ReadyForReview";
            if (candidates.Length == 1 && (contestedBanks.Contains(candidates[0])
                || bankOwners[candidates[0].ToString()].Any(x => x != sourceId)))
            {
                status = "blocked"; reason = "BankUsed";
            }
            var options = candidates.Where(x => !bankOwners[x.ToString()].Any(id => id != sourceId)).ToArray();
            if (options.Length == 0 && candidates.Length > 0) { status = "blocked"; reason = "BankUsed"; }
            result.Add(FinancialProposal(sourceId, ExternalMappingTypeEnum.حساب_بانکی, nameof(BankAccount),
                "BankAccount", status, reason, status == "pending" ? candidates[0] : (int?)null,
                status == "blocked" ? candidates : options, bankNotes[sourceId]));
        }

        var customerMaps = mappings.Where(x => x.MapType == ExternalMappingTypeEnum.مشتری && x.EntityName == "Customer")
            .GroupBy(x => x.EntityId).Where(g => g.Count() == 1 && g.Single().ExEntityName == "Party")
            .Select(g => g.Single()).Where(x => int.TryParse(x.ExEntityId, out _)).ToList();
        var partyIds = customerMaps.Select(x => int.Parse(x.ExEntityId)).Distinct().ToArray();
        var partyDls = new Dictionary<int, int>();
        foreach (var batch in partyIds.Chunk(350))
        {
            var rows = await context.Parties.AsNoTracking().Where(x => batch.Contains(x.PartyId)
                    && x.IsCustomer && x.Dl != null && x.Dl.IsActive)
                .Select(x => new { x.PartyId, x.DLRef }).ToListAsync(ct);
            foreach (var x in rows) partyDls[x.PartyId] = x.DLRef;
        }
        var customerDls = customerMaps.Where(x => partyDls.ContainsKey(int.Parse(x.ExEntityId)))
            .ToDictionary(x => x.EntityId, x => partyDls[int.Parse(x.ExEntityId)]);
        var usedTargets = mappings.Where(x => x.MapType == ExternalMappingTypeEnum.واریزی)
            .Select(x => (x.ExEntityName, x.ExEntityId)).ToHashSet();
        var unsaved = deposits.Where(x => !depositMaps[x.Id].Any()).ToList();
        var eligible = unsaved.Where(x => customerDls.ContainsKey(x.CustomerId)
            && !(x.Type == PayTypeEnum.چکی && x.Collected)).ToList();

        // Query only relevant receipt dates, customer DLs and amounts; then require the
        // corresponding single receipt item or note item, not merely a matching header.
        var receiptMatches = new List<ReceiptMatch>();
        var noteMatches = new List<NoteMatch>();
        var dls = customerDls.Values.Distinct().ToArray();
        foreach (var batch in eligible.Select(x => x.Date.Date).Distinct().Chunk(200))
        {
            if (dls.Length > 0)
                receiptMatches.AddRange(await context.ReceiptHeaders.AsNoTracking()
                    .Where(x => x.CurrencyRef == currency && x.Type == 1 && (x.ItemType == 4 || x.ItemType == 8)
                        && batch.Contains(x.Date.Date) && dls.Contains(x.DlRef))
                    .Select(x => new ReceiptMatch(x.ReceiptHeaderId, x.DlRef, x.ItemType, x.Date, x.TotalAmount)).ToListAsync(ct));
            noteMatches.AddRange(await context.DebitCreditNotes.AsNoTracking()
                .Where(x => x.CurrencyRef == currency && batch.Contains(x.Date.Date))
                .Select(x => new NoteMatch(x.DebitCreditNoteID, x.Date, x.SumAmount)).ToListAsync(ct));
        }
        var receiptsByKey = receiptMatches.ToLookup(x => (x.Dl, x.ItemType, x.Date.Date, x.Amount));
        var notesByKey = noteMatches.ToLookup(x => (x.Date.Date, x.Amount));
        var draftRows = new Dictionary<int, List<(int? Bank, decimal? Amount, DateTime? Date)>>();
        var chequeRows = new Dictionary<int, List<(bool Guarantee, int Type, decimal Amount, DateTime? Due)>>();
        foreach (var batch in receiptMatches.Select(x => x.Id).Distinct().Chunk(350))
        {
            var drafts = await context.ReceiptDrafts.AsNoTracking().Where(x => batch.Contains(x.ReceiptHeaderRef))
                .Select(x => new { x.ReceiptHeaderRef, x.BankAccountRef, x.Amount, x.Date }).ToListAsync(ct);
            foreach (var g in drafts.GroupBy(x => x.ReceiptHeaderRef))
                draftRows[g.Key] = g.Select(x => (x.BankAccountRef, x.Amount, x.Date)).ToList();
            var cheques = await context.ReceiptCheques.AsNoTracking().Where(x => batch.Contains(x.ReceiptHeaderRef))
                .Select(x => new { x.ReceiptHeaderRef, x.IsGuarantee, x.Type, x.Amount, x.Date }).ToListAsync(ct);
            foreach (var g in cheques.GroupBy(x => x.ReceiptHeaderRef))
                chequeRows[g.Key] = g.Select(x => (x.IsGuarantee, x.Type, x.Amount, x.Date)).ToList();
        }
        var noteRows = new Dictionary<int, List<(int? CreditType, int? Dl, decimal Amount)>>();
        foreach (var batch in noteMatches.Select(x => x.Id).Distinct().Chunk(350))
        {
            var items = await context.DebitCreditNoteItems.AsNoTracking().Where(x => batch.Contains(x.DebitCreditNoteRef))
                .Select(x => new { x.DebitCreditNoteRef, x.CreditType, x.CreditDLRef, x.Amount }).ToListAsync(ct);
            foreach (var g in items.GroupBy(x => x.DebitCreditNoteRef))
                noteRows[g.Key] = g.Select(x => (x.CreditType, x.CreditDLRef, x.Amount)).ToList();
        }
        var candidateRows = new List<(DepositMatchSource Source, string Entity, string Reason, int[] Ids)>();
        foreach (var source in unsaved)
        {
            var entity = source.Type == PayTypeEnum.تهاتری ? nameof(DebitCreditNote) : nameof(ReceiptHeader);
            var reason = "DepositNoMatch";
            int[] ids = [];
            if (source.Type == PayTypeEnum.چکی && source.Collected) reason = "DepositCollectedCheque";
            else if (!customerDls.TryGetValue(source.CustomerId, out var dl)) reason = "DepositNeedCustomer";
            else if (source.Type == PayTypeEnum.تهاتری)
                ids = notesByKey[(source.Date.Date, source.Amount)].Where(x =>
                    noteRows.TryGetValue(x.Id, out var rows) && rows.Count == 1 && rows[0].CreditType == 2
                    && rows[0].Dl == dl && rows[0].Amount == source.Amount).Select(x => x.Id).Distinct().ToArray();
            else if (source.Type == PayTypeEnum.چکی)
                ids = receiptsByKey[(dl, 4, source.Date.Date, source.Amount)].Where(x =>
                    source.Due.HasValue && chequeRows.TryGetValue(x.Id, out var rows) && rows.Count == 1
                    && !rows[0].Guarantee && rows[0].Type == 1 && rows[0].Amount == source.Amount
                    && rows[0].Due?.Date == source.Due.Value.Date).Select(x => x.Id).Distinct().ToArray();
            else
            {
                var saved = source.BankId.HasValue ? bankMaps[source.BankId.Value].ToArray() : [];
                if (saved.Length != 1 || saved[0].ExEntityName != "BankAccount"
                    || !int.TryParse(saved[0].ExEntityId, out var bankId) || !validBankIds.Contains(bankId))
                    reason = "DepositNeedBank";
                else
                    ids = receiptsByKey[(dl, 8, source.Date.Date, source.Amount)].Where(x =>
                        draftRows.TryGetValue(x.Id, out var rows) && rows.Count == 1
                        && rows[0].Bank == bankId && rows[0].Amount == source.Amount
                        && rows[0].Date?.Date == source.Date.Date).Select(x => x.Id).Distinct().ToArray();
            }
            candidateRows.Add((source, entity, reason, ids));
        }
        var contestedReceipts = candidateRows.SelectMany(x => x.Ids.Select(id => (x.Entity, Id: id, Source: x.Source.Id)))
            .GroupBy(x => (x.Entity, x.Id)).Where(g => g.Select(x => x.Source).Distinct().Count() > 1)
            .Select(g => g.Key).ToHashSet();
        foreach (var row in candidateRows)
        {
            var available = row.Ids.Where(id => !usedTargets.Contains((row.Entity, id.ToString()))).ToArray();
            var status = "notMatched";
            var reason = row.Reason;
            if (row.Reason == "DepositNeedCustomer" || row.Reason == "DepositNeedBank") status = "blocked";
            else if (row.Ids.Length > 0 && (available.Length == 0
                || available.Any(id => contestedReceipts.Contains((row.Entity, id)))))
            { status = "blocked"; reason = "DepositUsed"; }
            else if (row.Ids.Length > 1) { status = "ambiguous"; reason = "DepositAmbiguous"; }
            else if (available.Length == 1) { status = "pending"; reason = "ReadyForReview"; }
            result.Add(FinancialProposal(row.Source.Id, ExternalMappingTypeEnum.واریزی, nameof(Accountancy),
                row.Entity, status, reason, status == "pending" ? available[0] : (int?)null,
                status == "blocked" ? row.Ids : available,
                [$"واریزی {row.Source.Type} | {FormatLegacyPersianDate(row.Source.Date)} | {row.Source.Amount:N0} ریال"]));
        }
        return result;
    }

    private static MappingReportEntry FinancialProposal(Guid id, ExternalMappingTypeEnum type, string source, string target,
        string status, string reason, int? targetId, int[] candidates, string[] evidence) =>
        new($"financial-{(int)type}-{id}", source, id, status, reason, "", target,
            targetId, evidence, (int)type, targetId?.ToString(), candidates);
}

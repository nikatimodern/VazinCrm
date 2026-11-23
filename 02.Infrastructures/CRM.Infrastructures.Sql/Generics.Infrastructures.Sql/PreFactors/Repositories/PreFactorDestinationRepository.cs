using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Repositories;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using CRM.Framework;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

namespace CRM.Infrastructures.Sql.PreFactors.Repositories;

public sealed class PreFactorDestinationRepository(CRMContextDb crm, Sepidar1ContextDb first, Sepidar2ContextDb second)
    : IPreFactorDestinationRepository
{
    private const string OpenFiscalYearMessage = "سال مالی این پیش‌فاکتور در نرم‌افزار مقصد فعال نیست.";
    private const int OpenFiscalYear = 1;
    private const string PendingDelete = "حذف پیش‌فاکتور در نرم‌افزار مالی، در انتظار ثبت نتیجه";

    public async Task<SepidarPreFactorPrint?> GetPrintAsync(PreFactorDestinationKey key, CancellationToken ct)
    {
        var (destination, _, id, sourceNumber) = await ResolveAsync(key, ct);
        var header = await destination.Database.SqlQuery<SepidarPreFactorPrintHeader>($"""
            SELECT q.Number, q.Date, q.ExpirationDate, q.CurrencyTitle,
                COALESCE(NULLIF(q.CustomerRealName, N''), q.CustomerPartyName) AS BuyerName,
                q.CustomerEconomicCode AS BuyerEconomicCode, q.CustomerIdentificationCode AS BuyerRegistrationCode,
                q.PartyAddress AS BuyerAddress, q.CustomerZipCode AS BuyerPostalCode, q.CustomerPhone AS BuyerPhone,
                q.Price AS Subtotal, COALESCE(q.Addition,0) + COALESCE(q.AdditionFactor,0) AS Additions,
                COALESCE(q.Discount,0) AS Discount, COALESCE(q.Tax,0) AS Tax, COALESCE(q.Duty,0) AS Duty,
                COALESCE(q.NetPrice, q.Price - COALESCE(q.Discount,0) + COALESCE(q.Tax,0) + COALESCE(q.Duty,0)
                    + COALESCE(q.Addition,0) + COALESCE(q.AdditionFactor,0)) AS Total
            FROM SLS.vwQuotation q WHERE q.QuotationId = {id} AND q.Number = {sourceNumber}
            """).SingleOrDefaultAsync(ct);
        if (header == null)
            return null;

        var lines = await destination.Database.SqlQuery<SepidarPreFactorPrintLine>($"""
            SELECT RowID AS RowNumber, ItemCode AS Code, ItemTitle AS Title, Description,
                Quantity, UnitTitle AS Unit, Fee AS UnitPrice, Price AS Total
            FROM SLS.vwQuotationItem WHERE QuotationRef = {id}
            """).OrderBy(row => row.RowNumber).ToListAsync(ct);
        var seller = await GetPrintSellerAsync(destination, header.IsOfficial, ct);
        var note = await destination.ExtraData.AsNoTracking()
            .Where(x => x.EntityTypeName == "SG.Sales.InvoiceManagement.Common.DsQuotation" && x.EntityRef == id)
            .Select(x => x.Note).FirstOrDefaultAsync(ct);
        return new(header, seller, lines, note);
    }

    private static async Task<SepidarPreFactorSeller> GetPrintSellerAsync(
        SepidarContextDbBase destination, bool isOfficial, CancellationToken ct)
    {
        var templates = await destination.Database.SqlQuery<QuotationPrintTemplate>($"""
            SELECT UserReportsID AS Id, ReportData AS Layout,
                CONVERT(xml, XMLData).value('(/RgReport/LanguageTitle/LanguageEntry[@Culture="Farsi"]/@Value)[1]',
                    'nvarchar(200)') AS Title
            FROM FMK.UserReports
            WHERE ParentName = N'QuotationA4' AND ReportData IS NOT NULL AND XMLData IS NOT NULL
            """).OrderByDescending(x => x.Id).ToListAsync(ct);
        var template = templates.FirstOrDefault(x =>
        {
            var title = x.Title?.Replace('ي', 'ی').Replace(" ", "").Replace("\u200c", "");
            if (title?.EndsWith("رسمی", StringComparison.Ordinal) != true)
                return false;
            var isUnofficialTemplate = title.EndsWith("غیررسمی", StringComparison.Ordinal);
            return isOfficial == !isUnofficialTemplate;
        });

        var settings = await destination.Database.SqlQuery<QuotationPrintSetting>($"""
            SELECT RTRIM([Key]) AS Name, [Value] AS Value FROM FMK.Configuration
            WHERE [Key] IN ('CompanyName','CompanyEconomicCode','CompanyRegistrationCode','CompanyNationalId',
                'CompanyPostCode','CompanyFax','CompanyAddress','CompanyPhone')
            """).ToDictionaryAsync(x => x.Name, x => x.Value?.Trim(), ct);

        // When the destination uses the standard report, its seller comes from company settings.
        if (template == null)
        {
            var name = settings.GetValueOrDefault("CompanyName");
            return isOfficial
                ? new(name, settings.GetValueOrDefault("CompanyEconomicCode"),
                    settings.GetValueOrDefault("CompanyRegistrationCode"), settings.GetValueOrDefault("CompanyNationalId"),
                    settings.GetValueOrDefault("CompanyPostCode"), settings.GetValueOrDefault("CompanyFax"),
                    settings.GetValueOrDefault("CompanyAddress"), settings.GetValueOrDefault("CompanyPhone"))
                : new(name, null, null, null, null, null, null, null);
        }

        using var layout = new System.IO.MemoryStream(template.Layout);
        var report = XDocument.Load(layout);
        var sellerBand = report.Descendants("ChildBand").FirstOrDefault(band =>
            band.Elements("TextObject").Any(field => (string?)field.Attribute("Name") == "Text8"));

        // Read the seller fields from the customized FastReport header without evaluating report scripts.
        string? GetField(string fieldName, string settingName)
        {
            var value = (string?)sellerBand?.Elements("TextObject")
                .FirstOrDefault(field => (string?)field.Attribute("Name") == fieldName)?.Attribute("Text");
            if (value == $"[RgParam{settingName}]")
                return settings.GetValueOrDefault(settingName);
            return value?.Trim();
        }

        return new(GetField("Text8", "CompanyName"), GetField("Text11", "CompanyEconomicCode"),
            GetField("Text20", "CompanyRegistrationCode"), GetField("Text101", "CompanyNationalId"),
            GetField("Text100", "CompanyPostCode"), GetField("Text86", "CompanyFax"),
            GetField("Text10", "CompanyAddress"), GetField("Text24", "CompanyPhone"));
    }

    private sealed record QuotationPrintSetting(string Name, string? Value);
    private sealed record QuotationPrintTemplate(int Id, byte[] Layout, string? Title);

    public async Task<IReadOnlyList<PreFactorRelatedInvoice>> GetRelatedInvoicesAsync(PreFactorDestinationKey key, CancellationToken ct)
    {
        var (destination, _, id, sourceNumber) = await ResolveAsync(key, ct);
        if (!await destination.Quotations.AnyAsync(x => x.QuotationId == id && x.Number == sourceNumber, ct))
            throw new PreFactorDestinationException("پیش‌فاکتور مرتبط در نرم‌افزار مقصد پیدا نشد.");

        var linkedInvoices =
            from line in destination.SalesInvoiceItems
            join row in destination.QuotationItems on line.QuotationItemRef equals (int?)row.QuotationItemID
            where row.QuotationRef == id
            select line.InvoiceRef;
        var invoices = await destination.SalesInvoices.AsNoTracking()
            .Where(x => x.QuotationRef == id || linkedInvoices.Contains(x.InvoiceId))
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.InvoiceId)
            .Select(x => new PreFactorRelatedInvoice(x.InvoiceId, x.Number, x.Date, x.CustomerRealName, x.CreationDate, null, null))
            .ToListAsync(ct);
        if (invoices.Count == 0)
            return invoices;

        var externalIds = invoices.Select(x => x.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var mappings = await crm.ExternalMappings.AsNoTracking()
            .Where(x => x.ExternalSoftwareId == key.ExternalSoftwareId && x.MapType == ExternalMappingTypeEnum.فاکتور &&
                x.EntityName == nameof(Factor) && x.ExEntityName == nameof(Invoice) && externalIds.Contains(x.ExEntityId))
            .Select(x => new { x.ExEntityId, x.CreateDateTxt, x.UserCreator })
            .ToListAsync(ct);
        var registrationById = mappings.GroupBy(x => x.ExEntityId)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single());
        return invoices.ConvertAll(invoice =>
            registrationById.TryGetValue(invoice.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), out var registration)
                ? invoice with { RegisteredAt = registration.CreateDateTxt, RegisteredBy = registration.UserCreator }
                : invoice);
    }

    public async Task<PreFactorDestination?> GetAsync(PreFactorDestinationKey key, CancellationToken ct)
    {
        var (destination, mapping, id, sourceNumber) = await ResolveAsync(key, ct);
        var quote = await destination.Quotations.AsNoTracking().SingleOrDefaultAsync(x => x.QuotationId == id, ct);
        if (quote == null)
            return null;
        CheckReference(quote, sourceNumber);

        var blockers = await GetDeleteBlockersAsync(destination, quote, ct);
        var restriction = await GetRestrictionAsync(destination, quote, ct);
        var rows = await (
            from item in destination.QuotationItems.AsNoTracking()
            join product in destination.Items.AsNoTracking() on item.ItemRef equals product.ItemID
            join stock in destination.Stocks.AsNoTracking() on item.StockRef equals stock.StockID
            where item.QuotationRef == id
            orderby item.RowID, item.QuotationItemID
            select new { Item = item, ItemTitle = product.Code + " - " + product.Title, StockTitle = stock.Title })
            .ToListAsync(ct);
        var delivered = await destination.Database.SqlQuery<DeliveredLine>(
            $"SELECT QuotationItemID AS Id, CAST(DeliveredQuantity AS decimal(19,4)) AS Quantity FROM SLS.vwQuotationItem WHERE QuotationRef = {id}")
            .ToDictionaryAsync(x => x.Id, x => x.Quantity, ct);
        return new(quote.Number, mapping.ExternalSoftware.Title, quote.Version, quote.LastModificationDate,
            quote.SaleTypeRef, quote.CustomerPartyRef, quote.CustomerRealName, blockers.Length == 0,
            restriction, blockers, rows.Select(x => new PreFactorDestinationRow(x.Item.QuotationItemID,
                x.Item.RowID, x.Item.ItemRef, x.ItemTitle, x.Item.StockRef,
                x.StockTitle, x.Item.Quantity, x.Item.Fee,
                x.Item.UsedQuantity ?? 0m, delivered.GetValueOrDefault(x.Item.QuotationItemID),
                x.Item.TracingRef == null && x.Item.ProductPackRef == null && x.Item.SecondaryQuantity == null,
                x.Item.TracingRef == null)).ToArray());
    }

    public async Task SaveAsync(PreFactorDestinationEdit edit, int externalUserId, CancellationToken ct)
    {
        await using var mappingTransaction = await crm.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var (destination, _, id, sourceNumber) = await ResolveAsync(edit.Key, ct);
        await using var transaction = await destination.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var quote = await LockAsync(destination, id, ct);
        if (quote == null)
            throw new PreFactorDestinationException("پیش‌فاکتور در نرم‌افزار مقصد پیدا نشد.");

        CheckReference(quote, sourceNumber);
        CheckVersion(quote, edit.Version, edit.LastModificationDate);
        await CheckFiscalYearAsync(destination, quote, ct);
        var items = await destination.QuotationItems.Where(x => x.QuotationRef == id).ToListAsync(ct);
        if (edit.Items == null || edit.Items.Count != items.Count || edit.Items.Any(x => x == null) ||
            edit.Items.Select(x => x.Id).Distinct().Count() != items.Count)
            throw new PreFactorDestinationException("انتخاب‌های اقلام با ردیف‌های پیش‌فاکتور مقصد مطابقت ندارند.");

        var choices = edit.Items.ToDictionary(x => x.Id);
        if (items.Any(x => !choices.ContainsKey(x.QuotationItemID)))
            throw new PreFactorDestinationException("انتخاب‌های اقلام با ردیف‌های پیش‌فاکتور مقصد مطابقت ندارند.");

        if (edit.CustomerId != quote.CustomerPartyRef)
        {
            if ((await GetDeleteBlockersAsync(destination, quote, ct)).Length > 0)
                throw new PreFactorDestinationException("مشتری پیش‌فاکتورِ دارای سند وابسته قابل تغییر نیست.");
            var customer = await destination.Parties.AsNoTracking()
                .Where(x => x.PartyId == edit.CustomerId && x.IsCustomer && x.Dl.IsActive)
                .Select(x => new { x.PartyId, Name = x.Name + " " + x.LastName,
                    AddressId = x.PartyAddresses.Where(a => a.IsMain == 1).Select(a => (int?)a.PartyAddressId).FirstOrDefault() })
                .SingleOrDefaultAsync(ct)
                ?? throw new PreFactorDestinationException("مشتری معادل در نرم‌افزار مقصد معتبر نیست.");
            quote.CustomerPartyRef = customer.PartyId;
            quote.CustomerRealName = customer.Name;
            quote.CustomerRealName_En = customer.Name;
            quote.PartyAddressRef = customer.AddressId;
        }

        if (edit.SaleTypeId != quote.SaleTypeRef)
        {
            if (!await destination.SaleTypes.AnyAsync(x => x.SaleTypeId == edit.SaleTypeId, ct))
                throw new PreFactorDestinationException("نوع فروش در نرم‌افزار مقصد معتبر نیست.");
            quote.SaleTypeRef = edit.SaleTypeId;
        }

        var changedRows = items.Where(row => choices[row.QuotationItemID].ItemId != row.ItemRef ||
            choices[row.QuotationItemID].StockId != row.StockRef).ToArray();
        if (changedRows.Length > 0)
        {
            var selectedItems = changedRows.Select(x => choices[x.QuotationItemID].ItemId).Distinct().ToArray();
            var selectedStocks = changedRows.Select(x => choices[x.QuotationItemID].StockId).Distinct().ToArray();
            var valid = await destination.ItemStocks.AsNoTracking()
                .Where(x => selectedItems.Contains(x.ItemRef) && selectedStocks.Contains(x.StockRef) &&
                    x.Item.IsActive && x.Item.Sellable && x.Stock.IsActive)
                .Select(x => new { x.ItemRef, x.StockRef }).ToListAsync(ct);
            var validPairs = valid.Select(x => (x.ItemRef, x.StockRef)).ToHashSet();
            foreach (var row in changedRows)
            {
                var choice = choices[row.QuotationItemID];
                if (!validPairs.Contains((choice.ItemId, choice.StockId)))
                    throw new PreFactorDestinationException($"کالا یا انبار انتخاب‌شده برای ردیف {row.RowID} معتبر نیست.");
                if (row.TracingRef != null || choice.ItemId != row.ItemRef &&
                    (row.ProductPackRef != null || row.SecondaryQuantity != null))
                    throw new PreFactorDestinationException($"این تغییر برای ردیف {row.RowID} که اطلاعات ردیابی، بسته‌بندی یا واحد فرعی دارد قابل انجام نیست.");
                if (choice.ItemId != row.ItemRef)
                {
                    row.ItemRef = choice.ItemId;
                    row.UsedQuantity = 0;
                }
                row.StockRef = choice.StockId;
            }
        }

        if (destination.ChangeTracker.HasChanges())
        {
            quote.LastModifier = externalUserId;
            quote.LastModificationDate = DateTime.Now;
            quote.Version = checked(quote.Version + 1);
            await destination.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        await mappingTransaction.CommitAsync(ct);
    }

    public async Task DeleteAsync(PreFactorDestinationDelete request, string userId, string userName, CancellationToken ct)
    {
        await using var mappingTransaction = await crm.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var (destination, mapping, id, sourceNumber) = await ResolveAsync(request.Key, ct);
        await using var transaction = await destination.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var quote = await LockAsync(destination, id, ct);
        if (quote == null)
        {
            await mappingTransaction.RollbackAsync(ct);
            if (!await CompleteInterruptedDeletionAsync(request.Key, ct))
                throw new PreFactorDestinationException("سند در سپیدار پیدا نشد. از حذف سابقه ارسال استفاده کنید.");
            return;
        }

        CheckReference(quote, sourceNumber);
        CheckVersion(quote, request.Version, request.LastModificationDate);
        await CheckFiscalYearAsync(destination, quote, ct);
        var blockers = await GetDeleteBlockersAsync(destination, quote, ct);
        if (blockers.Length > 0)
            throw new PreFactorDestinationException("ابتدا سندهای وابسته را بررسی کنید. " + string.Join("، ", blockers));

        try
        {
            // A single DELETE allows Sepidar's own foreign keys to remove owned rows atomically.
            await destination.Quotations.Where(x => x.QuotationId == id).ExecuteDeleteAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            throw new PreFactorDestinationException("یک سند وابسته در نرم‌افزار مقصد مانع حذف پیش‌فاکتور است.");
        }

        var audit = await crm.logSystems.SingleOrDefaultAsync(x => x.Id == mapping.Id, ct);
        if (audit == null)
        {
            audit = new LogSystem { Id = mapping.Id, EntityName = nameof(Quotation), EntityId = mapping.ExEntityId,
                EntitySerialNumber = quote.Number, ParentEntityName = nameof(PreFactor), ParentEntityId = mapping.EntityId.ToString() };
            crm.logSystems.Add(audit);
        }
        audit.ChangedById = userId;
        audit.ChangedBy = userName;
        audit.ChangeTime = DateTime.Now;
        audit.ChangeTimeTxt = BaseServices.GetDateTimePersian();
        audit.ActionType = LogActionEnum.تغییر_وضعیت;
        audit.ActionTypeTxt = "درخواست حذف";
        audit.CommentTxt = mapping.ExternalSoftware.Title;
        audit.Description = PendingDelete;
        await crm.SaveChangesAsync(ct);
        // Persist the deletion intent before the destination commits so cleanup can be retried.
        await mappingTransaction.CommitAsync(ct);

        await transaction.CommitAsync(ct);
        await CompleteDeletionAsync(mapping, audit, ct);
    }

    public async Task<bool> CompleteInterruptedDeletionAsync(PreFactorDestinationKey key, CancellationToken ct)
    {
        var audit = await crm.logSystems.SingleOrDefaultAsync(x => x.Id == key.MappingId &&
            x.EntityName == nameof(Quotation) && x.Description == PendingDelete, ct);
        if (audit == null)
            return false;
        var (destination, mapping, id, _) = await ResolveAsync(key, ct);
        if (audit.EntityId != mapping.ExEntityId || audit.ParentEntityId != mapping.EntityId.ToString())
            return false;
        if (await destination.Quotations.AsNoTracking().AnyAsync(x => x.QuotationId == id, ct))
            return false;
        await CompleteDeletionAsync(mapping, audit, ct);
        return true;
    }

    private async Task CompleteDeletionAsync(ExternalMapping mapping, LogSystem audit, CancellationToken ct)
    {
        await using var transaction = await crm.Database.BeginTransactionAsync(ct);
        audit.ActionType = LogActionEnum.حذف;
        audit.ActionTypeTxt = "حذف";
        audit.Description = $"پیش‌فاکتور شماره {audit.EntitySerialNumber} در {audit.CommentTxt} حذف شد.";
        await crm.ExternalMappings.Where(x => x.Id == mapping.Id && x.EntityId == mapping.EntityId &&
            x.EntityName == mapping.EntityName && x.ExternalSoftwareId == mapping.ExternalSoftwareId &&
            x.MapType == mapping.MapType && x.ExEntityName == mapping.ExEntityName && x.ExEntityId == mapping.ExEntityId)
            .ExecuteDeleteAsync(ct);
        await crm.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task<(SepidarContextDbBase Context, ExternalMapping Mapping, int Id, int SourceNumber)> ResolveAsync(
        PreFactorDestinationKey key, CancellationToken ct)
    {
        SepidarContextDbBase destination = key.ExternalSoftwareId == Sepidar1SoftwareId ? first :
            key.ExternalSoftwareId == Sepidar2SoftwareId ? second :
            throw new PreFactorDestinationException("نرم‌افزار مقصد معتبر نیست.");
        var mapping = await crm.ExternalMappings.Include(x => x.ExternalSoftware).SingleOrDefaultAsync(x =>
            x.Id == key.MappingId && x.EntityId == key.SourceId && x.ExternalSoftwareId == key.ExternalSoftwareId &&
            x.EntityName == nameof(PreFactor) && x.MapType == ExternalMappingTypeEnum.پیش_فاکتور, ct);
        if (mapping?.ExEntityName != nameof(Quotation) || !int.TryParse(mapping.ExEntityId, out var id) || id <= 0)
            throw new PreFactorDestinationException("سابقه معتبر پیش‌فاکتور در ERP پیدا نشد.");
        if (await crm.ExternalMappings.AsNoTracking().AnyAsync(x => x.Id != mapping.Id &&
            x.ExternalSoftwareId == key.ExternalSoftwareId && x.ExEntityName == nameof(Quotation) &&
            x.ExEntityId == mapping.ExEntityId, ct))
            throw new PreFactorDestinationException("این سند مقصد به بیش از یک سابقه در ERP مرتبط است.");
        var sourceNumber = await crm.PreFactors.AsNoTracking().Where(x => x.Id == key.SourceId)
            .Select(x => (int?)x.SerialNumber).SingleOrDefaultAsync(ct)
            ?? throw new PreFactorDestinationException("پیش‌فاکتور مبدأ در ERP پیدا نشد.");
        return (destination, mapping, id, sourceNumber);
    }

    private static Task<Quotation?> LockAsync(SepidarContextDbBase context, int id, CancellationToken ct) =>
        context.Quotations.FromSql($"SELECT * FROM SLS.Quotation WITH (UPDLOCK,HOLDLOCK) WHERE QuotationId = {id}")
            .SingleOrDefaultAsync(ct);

    private static void CheckReference(Quotation quote, int sourceNumber)
    {
        if (quote.Number != sourceNumber)
            throw new PreFactorDestinationException("شماره پیش‌فاکتور در مقصد با شماره سند ERP مطابقت ندارد. ارتباط سند را بررسی کنید.");
    }

    private static void CheckVersion(Quotation quote, int version, DateTime lastModificationDate)
    {
        if (quote.Version != version || quote.LastModificationDate != lastModificationDate)
            throw new PreFactorDestinationException("پیش‌فاکتور بعد از باز شدن فرم تغییر کرده است. فرم را دوباره باز کنید.");
    }

    private static async Task<string?> GetRestrictionAsync(SepidarContextDbBase destination, Quotation quote, CancellationToken ct) =>
        await destination.FiscalYears.AnyAsync(year => year.FiscalYearId == quote.FiscalYearRef && year.Status == OpenFiscalYear, ct)
            ? null : OpenFiscalYearMessage;

    private static async Task CheckFiscalYearAsync(SepidarContextDbBase destination, Quotation quote, CancellationToken ct)
    {
        if (await GetRestrictionAsync(destination, quote, ct) is { } restriction)
            throw new PreFactorDestinationException(restriction);
    }

    private static async Task<string[]> GetDeleteBlockersAsync(SepidarContextDbBase destination, Quotation quote, CancellationToken ct)
    {
        var id = quote.QuotationId;
        var references = await destination.Database.SqlQuery<string>($"""
            SELECT DISTINCT TOP (10) Ref.Label AS [Value]
            FROM (
                SELECT N'فاکتور فروش شماره ' + CONVERT(nvarchar(30), i.Number) AS Label FROM SLS.Invoice i WHERE i.QuotationRef = {id}
                UNION ALL
                SELECT N'فاکتور فروش شماره ' + CONVERT(nvarchar(30), i.Number) FROM SLS.QuotationItem q
                    JOIN SLS.InvoiceItem l ON l.QuotationItemRef = q.QuotationItemID
                    JOIN SLS.Invoice i ON i.InvoiceId = l.InvoiceRef WHERE q.QuotationRef = {id}
                UNION ALL
                SELECT N'برگشت فروش شماره ' + CONVERT(nvarchar(30), i.Number) FROM SLS.ReturnedInvoice i WHERE i.QuotationRef = {id}
                UNION ALL
                SELECT N'خروج انبار شماره ' + CONVERT(nvarchar(30), d.Number) FROM SLS.QuotationItem q
                    JOIN INV.InventoryDeliveryItem l ON l.QuotationItemRef = q.QuotationItemID
                    JOIN INV.InventoryDelivery d ON d.InventoryDeliveryID = l.InventoryDeliveryRef WHERE q.QuotationRef = {id}
                UNION ALL
                SELECT N'سند تولید' FROM SLS.QuotationItem q
                    WHERE q.QuotationRef = {id} AND (
                        EXISTS (SELECT 1 FROM WKO.ProductOrder o WHERE o.BaseQuotationItemRef = q.QuotationItemID)
                        OR EXISTS (SELECT 1 FROM WKO.ProductOperation o WHERE o.BaseQuotationItemRef = q.QuotationItemID))
                UNION ALL
                SELECT N'ردیف پیش‌فاکتور دیگر' FROM SLS.QuotationItem q
                    JOIN SLS.QuotationItem l ON l.DiscountQuotationItemRef = q.QuotationItemID
                    WHERE q.QuotationRef = {id} AND l.QuotationRef <> {id}
            ) Ref ORDER BY Ref.Label
            """).ToListAsync(ct);
        if (quote.ReceiptRef != null) references.Add("رسید دریافت");
        if (quote.PaymentRef != null) references.Add("اعلامیه پرداخت");
        return references.ToArray();
    }

    private sealed record DeliveredLine(int Id, decimal Quantity);
}

using System.Globalization;
using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxReference;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Submission;

internal static class TaxReferenceInvoice
{
    internal static TaxReferenceRegistration Build(TaxReferenceSource source, TaxReferenceRequest request, TaxSigningBinding binding)
    {
        var reference = source.Reference;
        if (reference.Status != TaxDispatchStatus.Accepted || reference.Kind == TaxDispatchKind.Cancellation ||
            reference.AuthorityEnvironment != binding.Environment ||
            !TaxInvoicePayload.MatchesTaxId(reference.FiscalId, reference.TaxId, reference.InvoiceSerial))
            throw Invalid("مرجع باید دارای نتیجهٔ پردازش پذیرفته‌شده و شناسهٔ مالیاتی معتبر در همان محیط باشد.");

        using var payload = JsonDocument.Parse(source.Payload);
        var header = payload.RootElement.GetProperty("header");
        var prior = JsonSerializer.Deserialize<InvoicePreview>(source.Preview)
            ?? throw Invalid("پیش‌نمایش مرجع در دسترس نیست.");
        var pattern = prior.Pattern ?? TaxInvoicePattern.DomesticSale;
        if (prior.Errors.Count != 0 || prior.FiscalId != reference.FiscalId || prior.MemoryId != reference.MemoryId ||
            header.GetProperty("taxid").GetString() != reference.TaxId ||
            header.GetProperty("tins").GetString() != binding.SellerEconomicCode ||
            header.GetProperty("inty").GetInt32() != 1 || header.GetProperty("inp").GetInt32() != (int)pattern ||
            pattern is not (TaxInvoicePattern.DomesticSale or TaxInvoicePattern.Export))
            throw Invalid("هویت فروشنده، حافظه یا الگوی سند مرجع یکسان نیست.");

        var referenceTime = DateTimeOffset.FromUnixTimeMilliseconds(header.GetProperty("indatim").GetInt64());
        if (request.IssuedAt < referenceTime)
            throw Invalid("زمان صدور سند ارجاعی نباید پیش از مرجع باشد.");

        if (pattern == TaxInvoicePattern.Export && request.Kind == TaxDispatchKind.Return)
            throw Invalid("طبق V7.9 الگوی صادرات موضوع برگشت از فروش ندارد.");

        ValidateWorkFolder(source.WorkFolderEvidence, reference.TaxId, reference.Kind, request.Kind);
        var id = TaxInvoicePayload.TaxId(reference.FiscalId, request.Serial, request.IssuedAt);
        if (id == reference.TaxId || request.Serial == reference.InvoiceSerial)
            throw Invalid("برای سند ارجاعی شماره سریال مستقل وارد کنید.");

        if (request.Kind == TaxDispatchKind.Cancellation)
        {
            if (request.CancellationReason is not { } reason || !Enum.IsDefined(reason) ||
                request.Settlement.HasValue || request.CorrectsQuantityError || request.Export is not null)
                throw Invalid("برای ابطال، علت مستند و معتبر مشخص کنید.");
            var cancellation = JsonSerializer.Serialize(new
            {
                header = new
                {
                    taxid = id, indatim = request.IssuedAt.ToUnixTimeMilliseconds(),
                    inno = request.Serial.ToString("X10", CultureInfo.InvariantCulture),
                    irtaxid = reference.TaxId, ins = 3, tins = binding.SellerEconomicCode
                }
            });
            return Freeze(prior with { InvoiceDate = request.IssuedAt.Date }, cancellation, reason.ToString());
        }

        var candidate = InvoicePreview.Build(source.Candidate ??
            throw Invalid("فاکتور ارجاعی در دسترس نیست."), request.Kind == TaxDispatchKind.Return, request.Export);
        if (candidate.Errors.Count != 0)
            throw Invalid(string.Join(" ", candidate.Errors));
        if ((candidate.Pattern ?? TaxInvoicePattern.DomesticSale) != pattern || candidate.BuyerEconomicCode != prior.BuyerEconomicCode ||
            candidate.BuyerNationalId != prior.BuyerNationalId || candidate.BuyerPostalCode != prior.BuyerPostalCode ||
            prior.VatPercent != candidate.VatPercent || request.CancellationReason.HasValue ||
            candidate.InvoiceDate?.Date != request.IssuedAt.Date)
            throw Invalid("فاکتور ارجاعی، اطلاعات ثابت خریدار، نرخ مالیات یا تاریخ معتبر ندارد.");

        var priorSettlement = header.GetProperty("setm").GetInt32();
        if (request.Kind == TaxDispatchKind.Correction)
            ValidateCorrection(prior, candidate, request, priorSettlement);
        else
        {
            if (request.CorrectsQuantityError || (request.Settlement.HasValue && request.Settlement != priorSettlement))
                throw Invalid("در برگشت از فروش روش تسویه و مبنای سند مرجع حفظ می‌شود.");
            candidate = SubtractReturn(prior, candidate);
        }

        var filing = new TaxFilingDetails(request.Serial, request.IssuedAt, 0, request.Settlement ?? priorSettlement);
        var (_, json) = TaxInvoicePayload.Build(candidate, binding, filing, request.Kind, reference.TaxId);
        return Freeze(candidate, json, request.CorrectsQuantityError ? "QuantityEntryError" : null);

        TaxReferenceRegistration Freeze(InvoicePreview preview, string document, string? reason) =>
            new(JsonSerializer.Serialize(preview with
            {
                ReferenceTaxId = reference.TaxId, DispatchKind = request.Kind, ReferenceReason = reason, Version = null
            }), document, id, request.Serial, binding);
    }

    internal static void ValidateWorkFolder(string workFolderEvidence, string referenceTaxId,
        TaxDispatchKind referenceKind, TaxDispatchKind kind)
    {
        using var workFolder = JsonDocument.Parse(workFolderEvidence);
        var evidence = workFolder.RootElement;
        if (!evidence.TryGetProperty("taxId", out var taxId) || taxId.GetString() != referenceTaxId ||
            !evidence.TryGetProperty("invoiceStatus", out var state) ||
            (evidence.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null))
            throw Invalid("ابتدا وضعیت معتبر کارپوشهٔ سند مرجع را استعلام کنید.");
        var status = state.GetString();
        var valid = kind != TaxDispatchKind.Cancellation
            ? status is "APPROVED" or "SYSTEMIC_APPROVED" or "NO_NEED_REACTION"
            : status == "AWAITING_REACTION" || (referenceKind == TaxDispatchKind.Original &&
                status is "APPROVED" or "SYSTEMIC_APPROVED" or "NO_NEED_REACTION" or "REJECTED" or "SYSTEMIC_REJECTED");
        if (!valid)
            throw Invalid("وضعیت کارپوشهٔ مرجع برای این نوع سند ارجاعی مجاز نیست.");
    }

    private static void ValidateCorrection(InvoicePreview prior, InvoicePreview candidate, TaxReferenceRequest request, int settlement)
    {
        var previous = Index(prior.Lines);
        var changed = (request.Settlement.HasValue && request.Settlement != settlement) ||
            (prior.Pattern == TaxInvoicePattern.Export && ExportChanged(prior, candidate, previous));
        if (Index(candidate.Lines).Count != previous.Count)
            throw Invalid("در اصلاحیه شناسهٔ کالا یا واحد مرجع نباید اضافه یا حذف شود.");
        foreach (var line in candidate.Lines)
        {
            if (!previous.TryGetValue(Key(line), out var original) || line.ProductId != original.ProductId)
                throw Invalid("کالا و واحد اصلاحیه باید با مرجع یکسان باشد.");
            if (line.Quantity < original.Quantity && !request.CorrectsQuantityError)
                throw Invalid("کاهش مقدار در اصلاحیه فقط برای اصلاح اشتباه ثبت مقدار مجاز است.");
            changed |= (line.Quantity, line.UnitPrice, line.Discount) != (original.Quantity, original.UnitPrice, original.Discount);
        }
        if (!changed)
            throw Invalid("اصلاحیه باید دست‌کم یک تغییر قابل ارسال داشته باشد.");
    }

    private static bool ExportChanged(InvoicePreview prior, InvoicePreview candidate,
        IReadOnlyDictionary<(string?, string?), PreviewLine> previous)
    {
        var oldDetails = prior.ExportDetails ?? throw Invalid("اطلاعات گمرکی مرجع در دسترس نیست.");
        var newDetails = candidate.ExportDetails ?? throw Invalid("اطلاعات گمرکی اصلاحیه در دسترس نیست.");
        if (oldDetails.CustomsDeclarationDate is { } oldDate &&
            (newDetails.CustomsDeclarationDate is not { } newDate || newDate < oldDate))
            throw Invalid("تاریخ کوتاژ اصلاحیه نباید پیش از تاریخ کوتاژ مرجع باشد.");

        if ((oldDetails.CustomsDeclarationNumber, oldDetails.CustomsDeclarationDate, oldDetails.CustomsOffice) !=
            (newDetails.CustomsDeclarationNumber, newDetails.CustomsDeclarationDate, newDetails.CustomsOffice))
            return true;
        var oldLines = oldDetails.Lines.ToDictionary(x => x.FactorItemId);
        var newLines = newDetails.Lines.ToDictionary(x => x.FactorItemId);
        foreach (var line in candidate.Lines)
        {
            if (previous.TryGetValue(Key(line), out var oldLine) &&
                oldLines.TryGetValue(oldLine.FactorItemId.GetValueOrDefault(), out var oldCustoms) &&
                newLines.TryGetValue(line.FactorItemId.GetValueOrDefault(), out var newCustoms) &&
                (oldCustoms with { FactorItemId = newCustoms.FactorItemId }) != newCustoms)
                return true;
        }
        return false;
    }

    private static InvoicePreview SubtractReturn(InvoicePreview prior, InvoicePreview returned)
    {
        var remaining = Index(prior.Lines);
        foreach (var line in Index(returned.Lines).Values)
        {
            if (!remaining.TryGetValue(Key(line), out var original) || line.ProductId != original.ProductId ||
                line.Quantity > original.Quantity || line.UnitPrice != original.UnitPrice)
                throw Invalid("کالا، قیمت واحد یا مقدار برگشتی با مرجع مطابقت ندارد.");
            if (line.Quantity == original.Quantity)
            {
                if ((line.Discount, line.Vat, line.Total) != (original.Discount, original.Vat, original.Total))
                    throw Invalid("مبلغ ردیف کاملاً برگشتی با مرجع برابر نیست.");
                remaining.Remove(Key(line));
                continue;
            }
            remaining[Key(line)] = original with
            {
                Quantity = original.Quantity - line.Quantity, Discount = original.Discount - line.Discount,
                Vat = original.Vat - line.Vat, Total = original.Total - line.Total
            };
        }
        if (remaining.Count == 0)
            throw Invalid("برای برگشت تمام اقلام باید سند ابطالی صادر شود.");
        var lines = remaining.Values.OrderBy(x => x.Row).ToArray();
        return returned with
        {
            Lines = lines, Gross = lines.Sum(x => x.Quantity * x.UnitPrice), Discount = lines.Sum(x => x.Discount),
            Vat = lines.Sum(x => x.Vat), Total = lines.Sum(x => x.Total)
        };
    }

    private static Dictionary<(string?, string?), PreviewLine> Index(IReadOnlyList<PreviewLine> lines)
    {
        var grouped = lines.GroupBy(Key).ToArray();
        if (grouped.Any(x => x.Count() != 1))
            throw Invalid("تطبیق خودکار ردیف‌های تکراری با یک شناسه و واحد پشتیبانی نمی‌شود.");
        return grouped.ToDictionary(x => x.Key, x => x.Single());
    }

    private static (string?, string?) Key(PreviewLine line) => (line.GoodsId, line.TaxUnit);

    private static ArgumentException Invalid(string message) => new(message);
}

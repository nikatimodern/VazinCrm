using System.Globalization;
using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Submission;

public sealed record TaxFilingDetails(long Serial, DateTimeOffset IssuedAt, int BuyerType, int Settlement,
    TaxSettlementShares? Shares = null);

public static class TaxInvoicePayload
{
    private static readonly string[] Multiplication =
        ["0123456789", "1234067895", "2340178956", "3401289567", "4012395678",
         "5987604321", "6598710432", "7659821043", "8765932104", "9876543210"];
    private static readonly string[] Permutation =
        ["0123456789", "1576283094", "5803796142", "8916043527",
         "9453126870", "4286573901", "2793806415", "7046913258"];

    public static string TaxId(string fiscalId, long serial, DateTimeOffset issuedAt)
    {
        var days = issuedAt.ToUnixTimeSeconds() / 86400;
        if (fiscalId.Length != 6 || !fiscalId.All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c)) ||
            issuedAt < DateTimeOffset.UnixEpoch || days > 0xfffff || serial is <= 0 or > 0xffffffffff)
            throw Invalid();
        var memoryDigits = string.Concat(fiscalId.Select(c => char.IsAsciiDigit(c)
            ? c.ToString() : ((int)c).ToString(CultureInfo.InvariantCulture)));
        var digits = FormattableString.Invariant($"{memoryDigits}{days:D6}{serial:D12}");
        var check = 0;
        for (int i = 0; i < digits.Length; i++)
            check = Multiplication[check][Permutation[(i + 1) % 8][digits[^(i + 1)] - '0'] - '0'] - '0';
        return FormattableString.Invariant($"{fiscalId}{days:X5}{serial:X10}{"0432156789"[check]}");
    }

    public static bool MatchesTaxId(string fiscalId, string taxId, long? serial)
    {
        if (fiscalId is not { Length: 6 } || !fiscalId.All(c => char.IsAsciiDigit(c) || char.IsAsciiLetterUpper(c)) ||
            serial is not (> 0 and <= 0xffffffffff) || taxId is not { Length: 22 } ||
            !taxId.StartsWith(fiscalId, StringComparison.Ordinal) ||
            !int.TryParse(taxId.AsSpan(6, 5), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var days))
            return false;
        return TaxId(fiscalId, serial.Value, DateTimeOffset.UnixEpoch.AddDays(days)) == taxId;
    }

    public static (string TaxId, string Json) Build(InvoicePreview preview, TaxSigningBinding binding, TaxFilingDetails filing) =>
        Build(preview, binding, filing, TaxDispatchKind.Original, null);

    internal static (string TaxId, string Json) Build(InvoicePreview preview, TaxSigningBinding binding,
        TaxFilingDetails filing, TaxDispatchKind kind, string? referenceTaxId)
    {
        if (preview.Pattern == TaxInvoicePattern.Export)
            return TaxExportInvoice.Build(preview, binding, filing, kind, referenceTaxId);
        if ((preview.Pattern is not null and not TaxInvoicePattern.DomesticSale) ||
            (preview.InvoiceType is not null and not 1) || preview.ExportDetails is not null)
            throw new ArgumentException("داده‌های این نوع یا الگوی صورتحساب در ERP پشتیبانی نمی‌شود.");
        var original = kind == TaxDispatchKind.Original;
        if (!original && (kind is not (TaxDispatchKind.Correction or TaxDispatchKind.Return) ||
            referenceTaxId is not { Length: 22 }))
            throw Invalid();
        if (preview.Errors.Count != 0 || preview.FiscalId is null || preview.Lines.Count == 0 ||
            (original && filing.BuyerType is not (1 or 2)) ||
            filing.IssuedAt.Offset != TimeSpan.FromMinutes(210) ||
            filing.IssuedAt.Date != preview.InvoiceDate?.Date ||
            !Digits(binding.SellerEconomicCode, 11, 14) || !Digits(preview.BuyerEconomicCode, 11, 14) ||
            (original && !Digits(preview.BuyerNationalId, filing.BuyerType == 1 ? 10 : 11)) ||
            (!string.IsNullOrEmpty(preview.BuyerPostalCode) && !Digits(preview.BuyerPostalCode, 10)) ||
            preview.VatPercent is < 0 or > 100)
            throw Invalid();

        var taxId = TaxId(preview.FiscalId, filing.Serial, filing.IssuedAt);
        var body = preview.Lines.Select(line =>
        {
            if (!Digits(line.GoodsId, 13) || !TaxUnitCatalog.Contains(line.TaxUnit) || line.Description is { Length: > 400 })
                throw Invalid();
            if (TaxInvoiceAmounts.ValidateLine(line, preview.VatPercent, out var gross, out var net) is { } mismatch)
                throw new ArgumentException($"ردیف {line.Row} {mismatch}");
            return new { sstid = line.GoodsId, sstt = line.Description, mu = line.TaxUnit, am = line.Quantity,
                fee = line.UnitPrice, prdis = gross, dis = line.Discount, adis = net, vra = preview.VatPercent,
                vam = line.Vat, tsstam = line.Total };
        }).ToArray();

        if (!TaxInvoiceAmounts.TotalsMatch(preview, body.Sum(x => x.prdis), body.Sum(x => x.dis),
            body.Sum(x => x.vam), body.Sum(x => x.tsstam)))
            throw new ArgumentException("جمع ردیف‌های مالیاتی با مبالغ Snapshot مطابقت ندارد.");
        var shares = TaxInvoiceSettlement.Validate(filing.Settlement, preview.Gross - preview.Discount, filing.Shares);
        return (taxId, JsonSerializer.Serialize(new
        {
            header = new
            {
                taxid = taxId, inno = filing.Serial.ToString("X10", CultureInfo.InvariantCulture),
                indatim = filing.IssuedAt.ToUnixTimeMilliseconds(), inty = 1, inp = 1, ins = (int)kind + 1,
                irtaxid = referenceTaxId, tins = binding.SellerEconomicCode,
                tob = original ? (int?)filing.BuyerType : null, bid = original ? preview.BuyerNationalId : null,
                tinb = original ? preview.BuyerEconomicCode : null,
                bpc = original && !string.IsNullOrEmpty(preview.BuyerPostalCode) ? preview.BuyerPostalCode : null,
                tprdis = preview.Gross, tdis = preview.Discount, tadis = preview.Gross - preview.Discount,
                tvam = preview.Vat, todam = 0, tbill = preview.Total, setm = filing.Settlement,
                cap = shares?.Cash, insp = shares?.Credit
            },
            body,
            payments = Array.Empty<object>()
        }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
    }

    private static bool Digits(string? value, params int[] lengths) =>
        value is not null && lengths.Contains(value.Length) && value.All(char.IsAsciiDigit);

    private static ArgumentException Invalid() => new("اطلاعات صورتحساب با الگوی فروش داخلی پشتیبانی‌شده تطبیق ندارد.");
}

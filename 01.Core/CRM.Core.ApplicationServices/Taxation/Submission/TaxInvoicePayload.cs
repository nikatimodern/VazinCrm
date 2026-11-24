using System.Globalization;
using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Submission;

public sealed record TaxFilingDetails(long Serial, DateTimeOffset IssuedAt, int BuyerType, int Settlement);

public static class TaxInvoicePayload
{
    private const decimal MaximumMoney = 999999999999999999m;
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

    public static (string TaxId, string Json) Build(InvoicePreview preview, TaxSigningBinding binding, TaxFilingDetails filing)
    {
        if (preview.Errors.Count != 0 || preview.FiscalId is null || preview.Lines.Count == 0 ||
            filing.BuyerType is not (1 or 2) || filing.Settlement is not (1 or 2) ||
            filing.IssuedAt.Offset != TimeSpan.FromMinutes(210) ||
            filing.IssuedAt.Date != preview.InvoiceDate?.Date ||
            !Digits(binding.SellerEconomicCode, 11, 14) || !Digits(preview.BuyerEconomicCode, 11, 14) ||
            !Digits(preview.BuyerNationalId, filing.BuyerType == 1 ? 10 : 11) ||
            (!string.IsNullOrEmpty(preview.BuyerPostalCode) && !Digits(preview.BuyerPostalCode, 10)) ||
            preview.VatPercent is < 0 or > 100)
            throw Invalid();

        var taxId = TaxId(preview.FiscalId, filing.Serial, filing.IssuedAt);
        var body = preview.Lines.Select(line =>
        {
            if (line.Quantity <= 0 || line.Quantity > MaximumMoney ||
                decimal.Round(line.Quantity, 8) != line.Quantity ||
                line.UnitPrice <= 0 || line.UnitPrice > MaximumMoney || decimal.Round(line.UnitPrice, 8) != line.UnitPrice ||
                !Money(line.Discount) ||
                !Money(line.Vat) || !Money(line.Total) || !Digits(line.GoodsId, 13) ||
                line.TaxUnit is not { Length: > 0 and <= 8 } || !line.TaxUnit.All(char.IsAsciiDigit) ||
                line.Description is { Length: > 400 })
                throw Invalid();
            if (line.Quantity > decimal.MaxValue / Math.Max(1, line.UnitPrice))
                throw Invalid();
            var gross = line.Quantity * line.UnitPrice;
            var net = gross - line.Discount;
            if (gross == 0 || !Money(gross) || !Money(net) || line.Vat != net * preview.VatPercent / 100m ||
                line.Total != net + line.Vat)
                throw Invalid();
            return new { sstid = line.GoodsId, sstt = line.Description, mu = line.TaxUnit, am = line.Quantity,
                fee = line.UnitPrice, prdis = gross, dis = line.Discount, adis = net, vra = preview.VatPercent,
                vam = line.Vat, tsstam = line.Total };
        }).ToArray();

        if (!Money(preview.Gross) || !Money(preview.Discount) || !Money(preview.Vat) || !Money(preview.Total) ||
            body.Sum(x => x.prdis) != preview.Gross || body.Sum(x => x.dis) != preview.Discount ||
            body.Sum(x => x.vam) != preview.Vat || body.Sum(x => x.tsstam) != preview.Total)
            throw Invalid();
        return (taxId, JsonSerializer.Serialize(new
        {
            header = new
            {
                taxid = taxId, inno = filing.Serial.ToString("X10", CultureInfo.InvariantCulture),
                indatim = filing.IssuedAt.ToUnixTimeMilliseconds(), inty = 1, inp = 1, ins = 1,
                tins = binding.SellerEconomicCode, tob = filing.BuyerType, bid = preview.BuyerNationalId,
                tinb = preview.BuyerEconomicCode, bpc = string.IsNullOrEmpty(preview.BuyerPostalCode) ? null : preview.BuyerPostalCode,
                tprdis = preview.Gross, tdis = preview.Discount, tadis = preview.Gross - preview.Discount,
                tvam = preview.Vat, todam = 0, tbill = preview.Total, setm = filing.Settlement
            },
            body,
            payments = Array.Empty<object>()
        }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
    }

    private static bool Digits(string? value, params int[] lengths) =>
        value is not null && lengths.Contains(value.Length) && value.All(char.IsAsciiDigit);

    private static bool Money(decimal amount) => amount >= 0 && amount <= MaximumMoney && decimal.Truncate(amount) == amount;

    private static ArgumentException Invalid() => new("اطلاعات صورتحساب با الگوی فروش داخلی پشتیبانی‌شده تطبیق ندارد.");
}

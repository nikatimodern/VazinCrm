using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Submission;

internal static class TaxExportInvoice
{
    private const decimal MaximumCurrency = 99999999999999.9999m;
    private const decimal MaximumWeight = 999999999999999.99999999m;
    private static readonly TimeSpan IranOffset = TimeSpan.FromMinutes(210);
    private static readonly JsonSerializerOptions PayloadOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    internal static IEnumerable<string> Validate(InvoicePreview preview)
    {
        if (preview.ExportDetails is not { } details)
        {
            yield return "الگوی صادرات به ارزش ریالی و ارزی اظهارنامه، نرخ ارز و وزن خالص هر قلم نیاز دارد.";
            yield break;
        }

        if (!details.NoOtherTaxesOrDuties)
            yield return "عدم تعلق سایر مالیات، عوارض و وجوه قانونی باید از مستندات معامله تأیید شود.";
        if (preview.VatPercent != 0 || preview.Vat != 0 || preview.Lines.Any(x => x.Vat != 0))
            yield return "نرخ و مبلغ مالیات بر ارزش افزوده الگوی صادرات باید صفر باشد و با فاکتور ERP تطبیق کند.";
        if (details.CustomsDeclarationNumber is { } number &&
            (string.IsNullOrWhiteSpace(number) || number.Length > 14 || number != number.Trim()))
            yield return "شماره کوتاژ باید مطابق اظهارنامه و حداکثر ۱۴ نویسه باشد.";
        if (details.CustomsOffice is { } office && (office.Length != 5 || !office.All(char.IsAsciiDigit)))
            yield return "کد گمرک محل اظهار باید پنج رقم باشد.";
        if (details.CustomsDeclarationDate is { } date &&
            (EpochDay(date) is < 0 or > 99999 || date.ToOffset(IranOffset).Date > preview.InvoiceDate?.Date))
            yield return "تاریخ کوتاژ باید قابل ثبت و حداکثر برابر تاریخ صدور باشد.";

        if (details.Lines is null || details.Lines.Count != preview.Lines.Count ||
            details.Lines.Any(x => x is null || x.FactorItemId == Guid.Empty) ||
            details.Lines.Select(x => x.FactorItemId).Distinct().Count() != details.Lines.Count)
        {
            yield return "برای هر ردیف فاکتور باید دقیقاً یک ردیف اطلاعات اظهارنامه ارائه شود.";
            yield break;
        }

        var customs = details.Lines.ToDictionary(x => x.FactorItemId);
        var expectedCode = preview.Currency switch
        {
            nameof(CurrencyTypeEnum.دلار) => "USD",
            nameof(CurrencyTypeEnum.یورو) => "EUR",
            _ => null
        };
        if (expectedCode is null && preview.Currency != nameof(CurrencyTypeEnum.ریال))
            yield return "نوع ارز فاکتور در ERP پشتیبانی نمی‌شود.";
        if (details.Lines.Select(x => x.CurrencyCode).Distinct(StringComparer.Ordinal).Count() > 1)
            yield return "ارزش ارزی اقلام این صورتحساب باید در یک ارز واحد باشد.";

        foreach (var line in preview.Lines)
        {
            if (line.FactorItemId is not { } id || !customs.Remove(id, out var item))
            {
                yield return $"ردیف {line.Row} اطلاعات اظهارنامه متناظر ندارد.";
                continue;
            }

            if (!Positive(line.Quantity, TaxInvoiceAmounts.Maximum, 8) ||
                line.GoodsId is not { Length: 13 } || !line.GoodsId.All(char.IsAsciiDigit) ||
                line.TaxUnit is not { Length: > 0 and <= 8 } || !line.TaxUnit.All(char.IsAsciiDigit) ||
                line.Description is { Length: > 400 })
                yield return $"ردیف {line.Row} مقدار، شناسه کالا، واحد یا شرح معتبر ندارد.";
            if (item.CurrencyCode is not ("USD" or "EUR") || (expectedCode is not null && item.CurrencyCode != expectedCode))
                yield return $"ردیف {line.Row} کد ISO ارز اظهارنامه با ارز فاکتور یا دامنه ERP تطبیق ندارد.";
            if (!Positive(item.ExchangeRate, TaxInvoiceAmounts.Maximum, 8))
                yield return $"ردیف {line.Row} نرخ برابری ارز با ریال معتبر ندارد.";
            if (!Positive(item.RialValue, TaxInvoiceAmounts.Maximum, 0))
                yield return $"ردیف {line.Row} ارزش ریالی معتبر اظهارنامه ندارد.";
            if (!Positive(item.CurrencyValue, MaximumCurrency, 4))
                yield return $"ردیف {line.Row} ارزش ارزی اظهارنامه باید مثبت و حداکثر چهار رقم اعشار باشد.";
            if (!Positive(item.NetWeightKg, MaximumWeight, 8))
                yield return $"ردیف {line.Row} وزن خالص معتبر به کیلوگرم ندارد.";
        }
        if (customs.Count != 0)
            yield return "اطلاعات اظهارنامه شامل ردیفی خارج از فاکتور است.";

        // Totals use customs values, which can differ from the commercial invoice amounts.
        if (details.Lines.All(x => Positive(x.RialValue, TaxInvoiceAmounts.Maximum, 0) &&
            Positive(x.CurrencyValue, MaximumCurrency, 4) && Positive(x.NetWeightKg, MaximumWeight, 8)) &&
            details.Lines.Count != 0 &&
            (!Positive(details.Lines.Sum(x => x.RialValue), TaxInvoiceAmounts.Maximum, 0) ||
             !Positive(details.Lines.Sum(x => x.CurrencyValue), MaximumCurrency, 4) ||
             !Positive(details.Lines.Sum(x => x.NetWeightKg), MaximumWeight, 8)))
            yield return "جمع ارزش ریالی، ارزش ارزی یا وزن خالص در دامنهٔ الگوی صادرات نیست.";
    }

    internal static (string TaxId, string Json) Build(InvoicePreview preview, TaxSigningBinding binding,
        TaxFilingDetails filing, TaxDispatchKind kind, string? referenceTaxId)
    {
        if (preview.InvoiceType != 1 || preview.FiscalId is null || preview.Lines.Count == 0 ||
            filing.IssuedAt.Offset != IranOffset || filing.IssuedAt.Date != preview.InvoiceDate?.Date ||
            filing.Settlement is not (1 or 2) ||
            binding.SellerEconomicCode is not { Length: 11 or 14 } || !binding.SellerEconomicCode.All(char.IsAsciiDigit) ||
            (kind == TaxDispatchKind.Original ? referenceTaxId is not null :
                kind != TaxDispatchKind.Correction || referenceTaxId is not { Length: 22 }))
            throw new ArgumentException("نوع، موضوع، فروشنده، تاریخ یا تسویهٔ صورتحساب صادراتی معتبر نیست.");

        var errors = preview.Errors.Concat(Validate(preview)).Distinct().ToArray();
        if (errors.Length != 0)
            throw new ArgumentException(string.Join(" ", errors));

        var details = preview.ExportDetails!;
        if (details.CustomsDeclarationDate is { } date && date > filing.IssuedAt)
            throw new ArgumentException("زمان کوتاژ نباید پس از زمان صدور صورتحساب صادراتی باشد.");

        var customs = details.Lines.ToDictionary(x => x.FactorItemId);
        var body = preview.Lines.Select(line =>
        {
            var item = customs[line.FactorItemId!.Value];
            return new
            {
                sstid = line.GoodsId, sstt = line.Description, mu = line.TaxUnit, am = line.Quantity,
                cut = item.CurrencyCode, exr = item.ExchangeRate, nw = item.NetWeightKg,
                ssrv = item.RialValue, sscv = item.CurrencyValue, vra = 0, vam = 0, tsstam = item.RialValue
            };
        }).ToArray();

        var totalRial = body.Sum(x => x.ssrv);
        var id = TaxInvoicePayload.TaxId(preview.FiscalId, filing.Serial, filing.IssuedAt);
        return (id, JsonSerializer.Serialize(new
        {
            header = new
            {
                taxid = id, inno = filing.Serial.ToString("X10", CultureInfo.InvariantCulture),
                indatim = filing.IssuedAt.ToUnixTimeMilliseconds(), inty = 1, inp = (int)TaxInvoicePattern.Export,
                ins = (int)kind + 1, irtaxid = referenceTaxId, tins = binding.SellerEconomicCode,
                scc = details.CustomsOffice, cdcn = details.CustomsDeclarationNumber,
                cdcd = details.CustomsDeclarationDate is { } customsDate ? (int?)EpochDay(customsDate) : null,
                tonw = body.Sum(x => x.nw), torv = totalRial, tocv = body.Sum(x => x.sscv),
                tvam = 0, todam = 0, tbill = totalRial, setm = filing.Settlement
            },
            body,
            payments = Array.Empty<object>()
        }, PayloadOptions));
    }

    private static long EpochDay(DateTimeOffset date) =>
        date < DateTimeOffset.UnixEpoch ? -1 : date.ToUnixTimeSeconds() / 86400;

    private static bool Positive(decimal value, decimal max, int places) =>
        value > 0 && value <= max && decimal.Round(value, places) == value;
}

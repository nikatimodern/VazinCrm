namespace CRM.Core.ApplicationServices.Taxation.Dtos;

public enum TaxInvoicePattern { DomesticSale = 1, Export = 7 }

public sealed record ExportTaxLine(Guid FactorItemId, string CurrencyCode, decimal ExchangeRate,
    decimal RialValue, decimal CurrencyValue, decimal NetWeightKg);

public sealed record ExportTaxDetails(string? CustomsDeclarationNumber, DateTimeOffset? CustomsDeclarationDate,
    string? CustomsOffice, bool NoOtherTaxesOrDuties, IReadOnlyList<ExportTaxLine> Lines);

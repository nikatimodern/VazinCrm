using System;
using System.Collections.Generic;

namespace CRM.Core.Domain.PreFactors.Repositories;

public sealed record SepidarPreFactorPrint(SepidarPreFactorPrintHeader Header, SepidarPreFactorSeller Seller,
    IReadOnlyList<SepidarPreFactorPrintLine> Lines, string? Note);

public sealed record SepidarPreFactorPrintHeader(int Number, DateTime Date, DateTime ExpirationDate,
    string CurrencyTitle, string? BuyerName, string? BuyerEconomicCode, string? BuyerRegistrationCode,
    string? BuyerAddress, string? BuyerPostalCode, string? BuyerPhone,
    decimal Subtotal, decimal Additions, decimal Discount, decimal Tax, decimal Duty, decimal Total)
{
    public bool IsOfficial => Tax != 0;
}

public sealed record SepidarPreFactorSeller(string? Name, string? EconomicCode, string? RegistrationCode,
    string? NationalId, string? PostalCode, string? Fax, string? Address, string? Phone);

public sealed record SepidarPreFactorPrintLine(int RowNumber, string? Code, string? Title, string? Description,
    decimal Quantity, string? Unit, decimal UnitPrice, decimal Total);

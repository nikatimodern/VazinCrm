#nullable enable
namespace CRM.Core.Domain.Supplies.Models;
public enum SupplyResultKind
{
    Succeeded,
    Invalid,
    Missing,
    Unauthorized
}

// Payloads retain the existing query projection shapes, including legacy anonymous JSON records.
public sealed record SupplyResult(SupplyResultKind Kind, object? Data = null, string? Error = null, bool HasData = false)
{
    public static SupplyResult Success() => new(SupplyResultKind.Succeeded);
    public static SupplyResult Success(object? data) => new(SupplyResultKind.Succeeded, data, HasData: true);
    public static SupplyResult Invalid(string? error = null) => new(SupplyResultKind.Invalid, Error: error);
    public static SupplyResult Missing(string? error = null) => new(SupplyResultKind.Missing, Error: error);
    public static SupplyResult Unauthenticated() => new(SupplyResultKind.Unauthorized);
}

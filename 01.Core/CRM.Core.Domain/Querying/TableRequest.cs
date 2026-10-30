#nullable enable
namespace CRM.Core.Domain.Querying;
public sealed record TableRequest(string? Draw, int Start, int Length, string? SortColumn, string? SortDirection, string? Search);

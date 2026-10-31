using CRM.Core.Domain.Cartables.Repositories;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSupply;

namespace CRM.Core.ApplicationServices.Cartables.Abstractions;
public interface ICartablePresentation
{
    Task<object> PreFactorsAsync(CartablePreFactors data, CancellationToken ct);
    Task<object> FactorsAsync(CartableFactorData data, int passType, CancellationToken ct);
    Task<object> SupplyAsync(CartableSupplyResult data, CancellationToken ct);
    Task<object> FinancialAsync(CartableFinancialData data, CancellationToken ct);
    Task<object> SalesAsync(CartableSalesData data, CancellationToken ct);
}

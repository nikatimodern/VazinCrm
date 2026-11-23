#nullable enable
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Factors.Entities;

namespace CRM.Core.Domain.AccessCatalog.Repositories;

/// <summary>Request-local document access; apply before materializing lists, totals and exports.</summary>
public interface IDocumentReadAccess
{
    Task<IQueryable<T>> ApplyAsync<T>(IQueryable<T> query, string action, CancellationToken ct) where T : CRM.Framework.Domain.BaseEntity;
    bool IsEnforced(string entity);
    Task<IQueryable<PreFactor>> PreFactorsAsync(IQueryable<PreFactor> query, string action, CancellationToken ct);
    Task<IQueryable<Factor>> FactorsAsync(IQueryable<Factor> query, string action, CancellationToken ct);
}

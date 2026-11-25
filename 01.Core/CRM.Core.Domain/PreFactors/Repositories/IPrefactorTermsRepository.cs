using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.PreFactors.Repositories;

public sealed record PrefactorTerm(int Id, int Order, string Text);

public interface IPrefactorTermsRepository
{
    Task<List<PrefactorTerm>> GetAsync(CancellationToken ct);
    Task<bool> SaveAsync(int? id, int order, string text, string userId, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}

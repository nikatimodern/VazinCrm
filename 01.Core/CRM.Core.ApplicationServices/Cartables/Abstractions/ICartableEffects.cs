using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.PreFactors.Entities;

namespace CRM.Core.ApplicationServices.Cartables.Abstractions;
public interface ICartableEffects
{
    int ParseNumber(string? value);
    Task FillPreFactorDetailsAsync(IList<PreFactor> factors, CancellationToken ct);
    Task<object> GetEnvelopeAsync(AppUser user, int chats, CancellationToken ct);
}

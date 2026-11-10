using System.Collections.Generic;

namespace CRM.Core.Domain.AccessCatalog.Repositories;

public sealed record AccessModelOption(string DbContextName, string ModelFullName, string ModelName);

public interface IAccessModelCatalog
{
    IReadOnlyList<AccessModelOption> GetCandidates();
}

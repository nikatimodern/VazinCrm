namespace CRM.Core.ApplicationServices.AccessCatalog.Services;

public sealed record AccessModelOption(string DbContextName, string ModelFullName, string ModelName);

public interface IAccessModelCatalog
{
    IReadOnlyList<AccessModelOption> GetCandidates();
}

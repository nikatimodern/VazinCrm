using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.HumanResources.Entities;

public sealed class JobPosition : BaseEntity
{
    private JobPosition() { }

    public JobPosition(string code, string title)
    {
        Id = Guid.NewGuid();
        Code = EmployeeProfile.Required(code, 40, nameof(code));
        Rename(title);
        IsEnabled = true;
    }

    public string Code { get; private set; }
    public string Title { get; private set; }
    public Guid? LegacyPositionId { get; private set; }
    public bool IsEnabled { get; private set; }

    public void Rename(string title) => Title = EmployeeProfile.Required(title, 150, nameof(title));

    public void LinkLegacyPosition(Guid? legacyPositionId)
    {
        if (legacyPositionId == Guid.Empty)
            throw new ArgumentException("Invalid legacy position.", nameof(legacyPositionId));
        LegacyPositionId = legacyPositionId;
    }

    public void SetEnabled(bool enabled) => IsEnabled = enabled;
}

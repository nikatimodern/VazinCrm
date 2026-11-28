using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.HumanResources.Entities;

public sealed class Department : BaseEntity
{
    private Department() { }

    public Department(string code, string name, Guid? parentId = null)
    {
        Id = Guid.NewGuid();
        Code = EmployeeProfile.Required(code, 40, nameof(code));
        Rename(name);
        SetParent(parentId);
        IsEnabled = true;
    }

    public string Code { get; private set; }
    public string Name { get; private set; }
    public Guid? ParentId { get; private set; }
    public bool IsEnabled { get; private set; }

    public void Rename(string name) => Name = EmployeeProfile.Required(name, 150, nameof(name));

    // Ancestor cycles and parent existence must be checked transactionally by the use case.
    public void SetParent(Guid? parentId)
    {
        if (parentId == Guid.Empty || parentId == Id)
            throw new ArgumentException("Invalid parent department.", nameof(parentId));
        ParentId = parentId;
    }

    public void SetEnabled(bool enabled) => IsEnabled = enabled;
}

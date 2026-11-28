using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.HumanResources.Entities;

public sealed class EmployeeAssignment : BaseEntity
{
    private EmployeeAssignment() { }

    public EmployeeAssignment(Guid employeeId, Guid departmentId, Guid positionId,
        DateOnly effectiveFrom, Guid? managerEmployeeId = null, bool isPrimary = true)
    {
        if (employeeId == Guid.Empty) throw new ArgumentException("Employee is required.", nameof(employeeId));
        if (departmentId == Guid.Empty) throw new ArgumentException("Department is required.", nameof(departmentId));
        if (positionId == Guid.Empty) throw new ArgumentException("Position is required.", nameof(positionId));
        if (effectiveFrom == default) throw new ArgumentException("Effective date is required.", nameof(effectiveFrom));
        if (managerEmployeeId == Guid.Empty || managerEmployeeId == employeeId)
            throw new ArgumentException("Invalid manager.", nameof(managerEmployeeId));

        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        DepartmentId = departmentId;
        PositionId = positionId;
        ManagerEmployeeId = managerEmployeeId;
        EffectiveFrom = effectiveFrom;
        IsPrimary = isPrimary;
    }

    public Guid EmployeeId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid PositionId { get; private set; }
    public Guid? ManagerEmployeeId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsPrimary { get; private set; }

    // Effective dates are inclusive. A change creates a new assignment.
    // Overlap, organizational cycles and manager eligibility require transactional checks.
    public void Close(DateOnly effectiveTo)
    {
        if (EffectiveTo.HasValue) throw new InvalidOperationException("Assignment is already closed.");
        if (effectiveTo < EffectiveFrom) throw new ArgumentOutOfRangeException(nameof(effectiveTo));
        EffectiveTo = effectiveTo;
    }
}

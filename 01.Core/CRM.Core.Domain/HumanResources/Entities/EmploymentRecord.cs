using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.HumanResources.Entities;

public sealed class EmploymentRecord : BaseEntity
{
    private EmploymentRecord() { }

    public EmploymentRecord(Guid employeeId, DateOnly startDate, string employmentType)
    {
        if (employeeId == Guid.Empty) throw new ArgumentException("Employee is required.", nameof(employeeId));
        if (startDate == default) throw new ArgumentException("Start date is required.", nameof(startDate));
        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        StartDate = startDate;
        EmploymentType = EmployeeProfile.Required(employmentType, 64, nameof(employmentType));
    }

    public Guid EmployeeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string EmploymentType { get; private set; }

    // Inclusive end date; cross-record overlap and approval checked by use case.
    public void End(DateOnly endDate)
    {
        if (EndDate.HasValue) throw new InvalidOperationException("Employment record has already ended.");
        if (endDate < StartDate) throw new ArgumentOutOfRangeException(nameof(endDate));
        EndDate = endDate;
    }
}

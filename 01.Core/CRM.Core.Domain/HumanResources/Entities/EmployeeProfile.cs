using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.HumanResources.Entities;

public sealed class EmployeeProfile : BaseEntity
{
    private EmployeeProfile() { }

    public EmployeeProfile(string personnelCode, string firstName, string lastName, string appUserId = null)
    {
        Id = Guid.NewGuid();
        PersonnelCode = Required(personnelCode, 40, nameof(personnelCode));
        SetName(firstName, lastName);
        LinkAccount(appUserId);
    }

    public string PersonnelCode { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string AppUserId { get; private set; }

    public void SetName(string firstName, string lastName)
    {
        var first = Required(firstName, 100, nameof(firstName));
        var last = Required(lastName, 100, nameof(lastName));
        FirstName = first;
        LastName = last;
    }

    // Account existence, uniqueness and actor permission are application checks.
    public void LinkAccount(string appUserId)
    {
        AppUserId = appUserId is null ? null : Required(appUserId, 450, nameof(appUserId));
    }

    internal static string Required(string value, int limit, string parameter)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized) || normalized.Length > limit)
            throw new ArgumentException("Required value is empty or exceeds the allowed length.", parameter);
        return normalized;
    }
}

using System;

namespace CRM.Core.Domain.HumanResources.Entities;

// Optional HR-only data; not an EF entity and never written to Identity.
public sealed record EmployeePersonalDetails(
    string FatherName = null,
    string NationalCode = null,
    string BirthCertificateNumber = null,
    DateOnly? BirthDate = null,
    string Mobile = null,
    string Telephone = null,
    string Email = null,
    string Address = null,
    string Education = null,
    string EmergencyContactName = null,
    string EmergencyContactPhone = null);

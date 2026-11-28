using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using CRM.Core.Domain.HumanResources.Entities;

namespace CRM.EndPoint.FrontEnd.Models.HumanResources;

public sealed class EmployeeProfileForm
{
    public Guid? Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string AppUserId { get; set; }
    public byte[] RowVersion { get; set; }
    public string FatherName { get; set; }
    public string NationalCode { get; set; }
    public string BirthCertificateNumber { get; set; }
    public string BirthDateText { get; set; }
    public string Mobile { get; set; }
    public string Telephone { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string Education { get; set; }
    public string EmergencyContactName { get; set; }
    public string EmergencyContactPhone { get; set; }

    public string Latitude { get; set; }
    public string Longitude { get; set; }
    public IFormFile EducationDocument { get; set; }

    private static bool TryCoordinate(string text, out decimal? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var parsed)) return false;
        value = parsed;
        return true;
    }

    public bool TryGetDetails(out EmployeePersonalDetails details)
    {
        details = null;
        if (!HrPersianDate.TryParseOptional(BirthDateText, out var birthDate) ||
            !TryCoordinate(Latitude, out var latitude) || !TryCoordinate(Longitude, out var longitude)) return false;
        details = new EmployeePersonalDetails(FatherName, NationalCode, BirthCertificateNumber, birthDate,
            Mobile, Telephone, Email, Address, Education, EmergencyContactName, EmergencyContactPhone, latitude, longitude);
        return true;
    }
}

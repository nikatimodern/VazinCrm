using System;
using System.Globalization;
using System.Linq;
using CRM.Core.Domain.HumanResources.Entities;

namespace CRM.EndPoint.FrontEnd.Models.HumanResources;

public sealed class EmployeeProfileForm
{
    public Guid? Id { get; set; }
    public string PersonnelCode { get; set; }
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

    public bool TryGetDetails(out EmployeePersonalDetails details)
    {
        details = null;
        DateOnly? birthDate = null;
        if (!string.IsNullOrWhiteSpace(BirthDateText))
        {
            var normalized = new string(BirthDateText.Trim().Select(c => c switch
            {
                >= '\u06F0' and <= '\u06F9' => (char)('0' + c - '\u06F0'),
                >= '\u0660' and <= '\u0669' => (char)('0' + c - '\u0660'),
                _ => c
            }).ToArray());
            var parts = normalized.Split('/');
            if (parts.Length != 3 || parts[0].Length != 4 ||
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month) ||
                !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var day))
                return false;
            try { birthDate = DateOnly.FromDateTime(new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0)); }
            catch (ArgumentOutOfRangeException) { return false; }
        }
        details = new EmployeePersonalDetails(FatherName, NationalCode, BirthCertificateNumber, birthDate,
            Mobile, Telephone, Email, Address, Education, EmergencyContactName, EmergencyContactPhone);
        return true;
    }
}

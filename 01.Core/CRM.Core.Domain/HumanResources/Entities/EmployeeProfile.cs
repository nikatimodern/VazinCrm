using System;
using System.Linq;
using System.Net.Mail;
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

    public string FatherName { get; private set; }
    public string NationalCode { get; private set; }
    public string BirthCertificateNumber { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string Mobile { get; private set; }
    public string Telephone { get; private set; }
    public string Email { get; private set; }
    public string Address { get; private set; }
    public string Education { get; private set; }
    public string EmergencyContactName { get; private set; }
    public string EmergencyContactPhone { get; private set; }

    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public byte[] EducationDocumentContent { get; private set; }
    public string EducationDocumentType { get; private set; }

    // Only called by the separately authorized document workflow.
    public void SetEducationDocument(byte[] content)
    {
        if (content is null || content.Length < 8 || content.Length > 5 * 1024 * 1024)
            throw new ArgumentException("Invalid document size.", nameof(content));
        var type = content.AsSpan().StartsWith(new byte[] { 37, 80, 68, 70, 45 }) ? "application/pdf"
            : content.AsSpan().StartsWith(new byte[] { 255, 216, 255 }) ? "image/jpeg"
            : content.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? "image/png" : null;
        if (type is null) throw new ArgumentException("Unsupported document format.", nameof(content));
        EducationDocumentContent = (byte[])content.Clone();
        EducationDocumentType = type;
    }

    public void SetPersonalDetails(EmployeePersonalDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        var father = Optional(details.FatherName, 100, nameof(details.FatherName));
        var national = NormalizeNationalCode(details.NationalCode);
        var certificate = Optional(details.BirthCertificateNumber, 32, nameof(details.BirthCertificateNumber));
        var mobile = NormalizePhone(details.Mobile, nameof(details.Mobile));
        var telephone = NormalizePhone(details.Telephone, nameof(details.Telephone));
        var email = Optional(details.Email, 254, nameof(details.Email));
        if (email != null && (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email))
            throw new ArgumentException("Invalid email address.", nameof(details.Email));
        var address = Optional(details.Address, 500, nameof(details.Address));
        var education = Optional(details.Education, 100, nameof(details.Education));
        var emergencyName = Optional(details.EmergencyContactName, 150, nameof(details.EmergencyContactName));
        var emergencyPhone = NormalizePhone(details.EmergencyContactPhone, nameof(details.EmergencyContactPhone));
        if (details.BirthDate.HasValue && (details.BirthDate.Value.Year < 1900 || details.BirthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))))
            throw new ArgumentException("Invalid birth date.", nameof(details.BirthDate));

        if (details.Latitude.HasValue != details.Longitude.HasValue ||
            details.Latitude is < -90 or > 90 || details.Longitude is < -180 or > 180)
            throw new ArgumentException("Invalid address coordinates.", nameof(details));
        Latitude = details.Latitude.HasValue ? Math.Round(details.Latitude.Value, 6) : null;
        Longitude = details.Longitude.HasValue ? Math.Round(details.Longitude.Value, 6) : null;
        FatherName = father;
        NationalCode = national;
        BirthCertificateNumber = certificate;
        BirthDate = details.BirthDate;
        Mobile = mobile;
        Telephone = telephone;
        Email = email;
        Address = address;
        Education = education;
        EmergencyContactName = emergencyName;
        EmergencyContactPhone = emergencyPhone;
    }

    private static string Optional(string value, int max, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Required(value, max, parameter);
    }

    private static string NormalizeDigits(string value) => new(value.Select(c => c switch
    {
        >= '\u06F0' and <= '\u06F9' => (char)('0' + c - '\u06F0'),
        >= '\u0660' and <= '\u0669' => (char)('0' + c - '\u0660'),
        _ => c
    }).ToArray());

    private static string NormalizeNationalCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var code = NormalizeDigits(value.Trim());
        if (code.Length != 10 || code.Any(c => c < '0' || c > '9') || code.Distinct().Count() == 1)
            throw new ArgumentException("Invalid national code.", nameof(value));
        var remainder = code.Take(9).Select((c, i) => (c - '0') * (10 - i)).Sum() % 11;
        if (code[9] - '0' != (remainder < 2 ? remainder : 11 - remainder))
            throw new ArgumentException("Invalid national code checksum.", nameof(value));
        return code;
    }

    private static string NormalizePhone(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var phone = NormalizeDigits(value.Trim());
        if (phone.Length > 24 || phone.Count(c => c >= '0' && c <= '9') < 3 ||
            phone.Where((c, i) => !(c >= '0' && c <= '9') && c != ' ' && c != '-' && c != '(' && c != ')' && !(c == '+' && i == 0)).Any())
            throw new ArgumentException("Invalid telephone number.", parameter);
        return phone;
    }

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

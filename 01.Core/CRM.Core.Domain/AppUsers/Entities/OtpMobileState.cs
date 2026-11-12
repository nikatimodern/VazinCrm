using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace CRM.Core.Domain.AppUsers.Entities;

public enum OtpActionStatus { Issued, Accepted, Invalid, Expired, Locked, Cooldown }

public sealed record OtpActionResult(OtpActionStatus Status, int RetryAfterSeconds = 0, int RemainingAttempts = 0);

public sealed class OtpMobileState
{
    [Key, MaxLength(11)]
    public string Mobile { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? OtpType { get; set; }
    public byte[]? CodeSalt { get; set; }
    public byte[]? CodeHash { get; set; }
    public DateTime? OtpExpiresAtUtc { get; set; }
    public int FailedAttempts { get; set; }
    public int LockoutCount { get; set; }
    public DateTime? LockedUntilUtc { get; set; }

    public OtpActionResult Issue(string type, string code, DateTime nowUtc)
    {
        if (IsLocked(nowUtc))
            return new(OtpActionStatus.Locked, Remaining(LockedUntilUtc!.Value, nowUtc));
        if (CodeHash is not null && OtpExpiresAtUtc > nowUtc)
            return new(OtpActionStatus.Cooldown, Remaining(OtpExpiresAtUtc.Value, nowUtc));

        CodeSalt = RandomNumberGenerator.GetBytes(16);
        CodeHash = Hash(code, CodeSalt);
        OtpType = type;
        OtpExpiresAtUtc = nowUtc.AddMinutes(1);
        FailedAttempts = 0;
        return new(OtpActionStatus.Issued, 60, 3);
    }

    public OtpActionResult Verify(string type, string? code, DateTime nowUtc)
    {
        if (IsLocked(nowUtc))
            return new(OtpActionStatus.Locked, Remaining(LockedUntilUtc!.Value, nowUtc));

        if (CodeHash is null || CodeSalt is null || OtpType != type || OtpExpiresAtUtc <= nowUtc)
            return new(OtpActionStatus.Expired);

        var matches = !string.IsNullOrWhiteSpace(code)
            && code.Length == 4
            && CryptographicOperations.FixedTimeEquals(CodeHash, Hash(code, CodeSalt));
        if (matches)
        {
            Invalidate();
            FailedAttempts = 0;
            return new(OtpActionStatus.Accepted);
        }

        FailedAttempts++;
        if (FailedAttempts < 3)
            return new(OtpActionStatus.Invalid, RemainingAttempts: 3 - FailedAttempts);

        LockoutCount++;
        var seconds = 60L + 30L * (LockoutCount - 1L);
        LockedUntilUtc = nowUtc.AddSeconds(seconds);
        Invalidate();
        return new(OtpActionStatus.Locked, Remaining(LockedUntilUtc.Value, nowUtc));
    }

    public OtpActionResult GetStatus(string type, DateTime nowUtc)
    {
        if (IsLocked(nowUtc))
            return new(OtpActionStatus.Locked, Remaining(LockedUntilUtc!.Value, nowUtc));
        return CodeHash is not null && OtpType == type && OtpExpiresAtUtc > nowUtc
            ? new(OtpActionStatus.Cooldown, Remaining(OtpExpiresAtUtc.Value, nowUtc), 3 - FailedAttempts)
            : new(OtpActionStatus.Expired);
    }

    public void CancelIfMatches(string type, string code)
    {
        if (OtpType == type && CodeSalt is not null && CodeHash is not null
            && CryptographicOperations.FixedTimeEquals(CodeHash, Hash(code, CodeSalt)))
            Invalidate();
    }

    public void ClearLockout()
    {
        LockoutCount = 0;
        LockedUntilUtc = null;
    }

    private bool IsLocked(DateTime utcNow) => LockedUntilUtc > utcNow;

    private void Invalidate()
    {
        CodeSalt = null;
        CodeHash = null;
        OtpExpiresAtUtc = null;
        OtpType = null;
    }

    private static byte[] Hash(string code, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(code), salt, 100_000, HashAlgorithmName.SHA256, 32);

    private static int Remaining(DateTime deadline, DateTime nowUtc) =>
        Math.Max(1, (int)Math.Ceiling((deadline - nowUtc).TotalSeconds));
}

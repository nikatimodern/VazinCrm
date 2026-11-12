using System;
using System.ComponentModel.DataAnnotations;

namespace CRM.Core.Domain.AppUsers.Entities;

public enum PasswordAttemptStatus
{
    Allowed,
    Invalid,
    Locked
}

public sealed record PasswordAttemptResult(
    PasswordAttemptStatus Status, int RetryAfterSeconds = 0, int RemainingAttempts = 0);

public sealed class PasswordLockoutState
{
    [Key, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int FailedAttempts { get; set; }
    public int LockoutCount { get; set; }
    public DateTime? LockedUntilUtc { get; set; }

    public PasswordAttemptResult GetStatus(DateTime utcNow)
    {
        return LockedUntilUtc > utcNow
            ? new(PasswordAttemptStatus.Locked,
                (int)Math.Ceiling((LockedUntilUtc.Value - utcNow).TotalSeconds))
            : new(PasswordAttemptStatus.Allowed);
    }

    public PasswordAttemptResult RegisterAttempt(bool passwordIsValid, DateTime utcNow)
    {
        var current = GetStatus(utcNow);
        if (current.Status == PasswordAttemptStatus.Locked) return current;

        if (passwordIsValid)
        {
            FailedAttempts = 0;
            LockoutCount = 0;
            LockedUntilUtc = null;
            return new(PasswordAttemptStatus.Allowed);
        }

        FailedAttempts++;
        if (FailedAttempts < 3)
            return new(PasswordAttemptStatus.Invalid, RemainingAttempts: 3 - FailedAttempts);

        FailedAttempts = 0;
        LockoutCount = checked(LockoutCount + 1);
        var durationSeconds = 60L + 30L * (LockoutCount - 1L);
        LockedUntilUtc = utcNow.AddSeconds(durationSeconds);
        return new(PasswordAttemptStatus.Locked,
            (int)Math.Ceiling((LockedUntilUtc.Value - utcNow).TotalSeconds));
    }
}

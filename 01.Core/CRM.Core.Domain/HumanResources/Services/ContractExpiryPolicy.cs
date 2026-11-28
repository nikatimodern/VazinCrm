using System;
using System.Collections.Generic;
using System.Linq;
using CRM.Core.Domain.HumanResources.Entities;

namespace CRM.Core.Domain.HumanResources.Services;

public enum ContractDateState
{
    Draft, Cancelled, Terminated, Upcoming, Valid, ExpiringSoon, EndsToday, Expired
}

public static class ContractExpiryPolicy
{
    // The caller supplies the company-local business date. End dates are inclusive.
    public static ContractDateState GetState(EmploymentContractStatus status, DateOnly startDate,
        DateOnly? endDate, DateOnly? terminatedOn, DateOnly today, int warningDays)
    {
        if (warningDays is < 0 or > 365) throw new ArgumentOutOfRangeException(nameof(warningDays));
        if (status == EmploymentContractStatus.Draft) return ContractDateState.Draft;
        if (status == EmploymentContractStatus.Cancelled) return ContractDateState.Cancelled;
        if (status == EmploymentContractStatus.Terminated && terminatedOn.HasValue && terminatedOn.Value <= today)
            return ContractDateState.Terminated;
        if (status != EmploymentContractStatus.Approved && status != EmploymentContractStatus.Terminated)
            throw new ArgumentOutOfRangeException(nameof(status));
        if (startDate > today) return ContractDateState.Upcoming;
        if (!endDate.HasValue) return ContractDateState.Valid;
        var remaining = endDate.Value.DayNumber - today.DayNumber;
        if (remaining < 0) return ContractDateState.Expired;
        if (remaining == 0) return ContractDateState.EndsToday;
        return remaining <= warningDays ? ContractDateState.ExpiringSoon : ContractDateState.Valid;
    }

    // Select the most urgent threshold already reached, enabling catch-up after
    // downtime without generating all missed notifications in one run.
    // Persisted deduplication and recipient authorization must precede delivery.
    public static int? GetReachedReminderThreshold(DateOnly endDate, DateOnly today,
        IReadOnlyCollection<int> thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);
        if (thresholds.Any(day => day is < 0 or > 365))
            throw new ArgumentOutOfRangeException(nameof(thresholds));
        var remaining = endDate.DayNumber - today.DayNumber;
        if (remaining < 0) return null; // Expired follow-up uses its own workflow.
        return thresholds.Where(day => day >= remaining).OrderBy(day => day)
            .Select(day => (int?)day).FirstOrDefault();
    }
}

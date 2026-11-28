using System;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.HumanResources.Entities;

public enum EmploymentContractTerm { FixedTerm = 1, OpenEnded = 2 }
public enum EmploymentContractStatus { Draft = 1, Approved = 2, Cancelled = 3, Terminated = 4 }

public sealed class EmploymentContract : BaseEntity
{
    private EmploymentContract() { }

    public EmploymentContract(Guid employeeId, string contractNumber, EmploymentContractTerm term,
        DateOnly agreementDate, DateOnly startDate, DateOnly? endDate, Guid? previousContractId = null)
    {
        if (employeeId == Guid.Empty) throw new ArgumentException("Employee is required.", nameof(employeeId));
        if (previousContractId == Guid.Empty) throw new ArgumentException("Invalid predecessor.", nameof(previousContractId));
        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        PreviousContractId = previousContractId;
        Status = EmploymentContractStatus.Draft;
        SetTerms(contractNumber, term, agreementDate, startDate, endDate);
    }

    public Guid EmployeeId { get; private set; }
    public Guid? PreviousContractId { get; private set; }
    public string ContractNumber { get; private set; }
    public EmploymentContractTerm Term { get; private set; }
    public DateOnly AgreementDate { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public EmploymentContractStatus Status { get; private set; }
    public string ApprovedByUserId { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public DateOnly? TerminatedOn { get; private set; }
    public string ResolutionReason { get; private set; }
    public string ResolvedByUserId { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    // Only drafts may be edited. Changes after approval require a separate
    // approved amendment or replacement workflow, not an overwritten history.
    public void SetTerms(string number, EmploymentContractTerm term, DateOnly agreementDate,
        DateOnly startDate, DateOnly? endDate)
    {
        RequireDraft();
        var validatedNumber = EmployeeProfile.Required(number, 64, nameof(number));
        if (!Enum.IsDefined(term)) throw new ArgumentOutOfRangeException(nameof(term));
        if (agreementDate == default || startDate == default)
            throw new ArgumentException("Agreement and start dates are required.");
        if (term == EmploymentContractTerm.FixedTerm && (!endDate.HasValue || endDate.Value < startDate))
            throw new ArgumentException("Fixed-term contracts require a valid end date.", nameof(endDate));
        if (term == EmploymentContractTerm.OpenEnded && endDate.HasValue)
            throw new ArgumentException("Open-ended contracts cannot have an end date.", nameof(endDate));
        ContractNumber = validatedNumber;
        Term = term;
        AgreementDate = agreementDate;
        StartDate = startDate;
        EndDate = endDate;
    }

    // Authorization, employee eligibility, predecessor and overlap validation
    // belong to the transactional approval use case.
    public void Approve(string actorId, DateTimeOffset approvedAt)
    {
        RequireDraft();
        var actor = EmployeeProfile.Required(actorId, 450, nameof(actorId));
        if (approvedAt == default) throw new ArgumentException("Approval timestamp is required.", nameof(approvedAt));
        ApprovedByUserId = actor;
        ApprovedAtUtc = approvedAt.ToUniversalTime();
        Status = EmploymentContractStatus.Approved;
    }

    public EmploymentContract CreateRenewalDraft(string number, EmploymentContractTerm term,
        DateOnly agreementDate, DateOnly startDate, DateOnly? endDate)
    {
        if (Status != EmploymentContractStatus.Approved || !EndDate.HasValue)
            throw new InvalidOperationException("Only approved fixed-term contracts can be renewed using this workflow.");
        if (startDate <= EndDate.Value)
            throw new ArgumentException("Renewal must start after the predecessor ends.", nameof(startDate));
        return new EmploymentContract(EmployeeId, number, term, agreementDate, startDate, endDate, Id);
    }

    public void CancelDraft(string actorId, string reason, DateTimeOffset at)
    {
        RequireDraft();
        RecordResolution(actorId, reason, at);
        Status = EmploymentContractStatus.Cancelled;
    }

    public void Terminate(DateOnly effectiveDate, string actorId, string reason, DateTimeOffset at)
    {
        if (Status != EmploymentContractStatus.Approved)
            throw new InvalidOperationException("Only approved contracts can be terminated.");
        if (effectiveDate < StartDate || (EndDate.HasValue && effectiveDate > EndDate.Value))
            throw new ArgumentOutOfRangeException(nameof(effectiveDate));
        RecordResolution(actorId, reason, at);
        TerminatedOn = effectiveDate;
        Status = EmploymentContractStatus.Terminated;
    }

    private void RecordResolution(string actorId, string reason, DateTimeOffset at)
    {
        var actor = EmployeeProfile.Required(actorId, 450, nameof(actorId));
        var text = EmployeeProfile.Required(reason, 1000, nameof(reason));
        if (at == default) throw new ArgumentException("Resolution timestamp is required.", nameof(at));
        ResolvedByUserId = actor;
        ResolutionReason = text;
        ResolvedAtUtc = at.ToUniversalTime();
    }

    private void RequireDraft()
    {
        if (Status != EmploymentContractStatus.Draft)
            throw new InvalidOperationException("Only draft contracts can be changed.");
    }
}

using System;
using CRM.Core.Domain.HumanResources.Entities;
using CRM.Core.Domain.HumanResources.Repositories;

namespace CRM.EndPoint.FrontEnd.Models.HumanResources;

public sealed class EmploymentContractForm
{
    public Guid EmployeeId { get; set; }
    public Guid? ContractId { get; set; }
    public ContractOperation Operation { get; set; }
    public string Number { get; set; }
    public EmploymentContractTerm Term { get; set; }
    public string AgreementDateText { get; set; }
    public string StartDateText { get; set; }
    public string EndDateText { get; set; }
    public string TerminationDateText { get; set; }
    public string Reason { get; set; }
    public byte[] RowVersion { get; set; }

    public bool TryGetChange(out ContractChange change)
    {
        change = null;
        if (EmployeeId == Guid.Empty || !Enum.IsDefined(Operation)) return false;
        DateOnly? agreement = null, start = null, end = null, termination = null;
        if (Operation is ContractOperation.Create or ContractOperation.Edit or ContractOperation.Renew)
        {
            if (!Enum.IsDefined(Term) || !HrPersianDate.TryParseOptional(AgreementDateText, out agreement) ||
                !HrPersianDate.TryParseOptional(StartDateText, out start) ||
                !HrPersianDate.TryParseOptional(EndDateText, out end) ||
                !agreement.HasValue || !start.HasValue)
                return false;
        }
        if (Operation == ContractOperation.Terminate &&
            (!HrPersianDate.TryParseOptional(TerminationDateText, out termination) || !termination.HasValue))
            return false;
        change = new ContractChange(EmployeeId, ContractId, Operation, Number, Term,
            agreement.GetValueOrDefault(), start.GetValueOrDefault(), end, termination, Reason, RowVersion);
        return true;
    }
}

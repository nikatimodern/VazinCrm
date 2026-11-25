using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.InquireTaxSubmission;

public sealed record InquireTaxSubmissionCommand(Guid DispatchId) : ICommand<TaxDispatchState>;

public sealed class InquireTaxSubmissionCommandHandler(ITaxSubmissionRepository repository, ITaxDispatchQueue queue,
    ITaxAuthorityClient client) : ICommandHandler<InquireTaxSubmissionCommand, TaxDispatchState>
{
    public async Task<TaxDispatchState> HandleAsync(InquireTaxSubmissionCommand request, CancellationToken ct)
    {
        var state = await queue.GetStateAsync(request.DispatchId, ct) ?? throw new KeyNotFoundException();
        if (state.Status is not (TaxDispatchStatus.Unknown or TaxDispatchStatus.Submitted))
            return state;

        var submission = await repository.FindAsync(request.DispatchId, ct)
            ?? throw new ArgumentException("برای این درخواست عملیات ارسال ثبت نشده است.");
        var result = await client.InquireAsync(submission, ct);
        if (result is not null)
            await repository.RecordProcessingAsync(request.DispatchId, state.AttemptId!.Value, result.ReferenceNumber,
                result.Status, result.Evidence, ct);
        else
            await repository.RecordInquiryWithoutResultAsync(request.DispatchId, state.AttemptId!.Value,
                TaxDispatchEventKind.Processing, ct);
        return await queue.GetStateAsync(request.DispatchId, ct);
    }
}

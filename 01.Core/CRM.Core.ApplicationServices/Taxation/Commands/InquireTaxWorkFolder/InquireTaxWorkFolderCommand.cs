using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.InquireTaxWorkFolder;

public sealed record InquireTaxWorkFolderCommand(Guid DispatchId) : ICommand<TaxWorkFolderState>;

public sealed record TaxWorkFolderState(string? InvoiceStatus, string? Article6Status, string? Error);

public sealed class InquireTaxWorkFolderCommandHandler(ITaxSubmissionRepository repository, ITaxAuthorityClient client)
    : ICommandHandler<InquireTaxWorkFolderCommand, TaxWorkFolderState>
{
    public async Task<TaxWorkFolderState> HandleAsync(InquireTaxWorkFolderCommand request, CancellationToken ct)
    {
        var submission = await repository.FindAsync(request.DispatchId, ct);
        if (submission is not { Status: TaxDispatchStatus.Accepted, AttemptId: { } attempt })
            throw new ArgumentException("ابتدا نتیجهٔ پردازش صورتحساب را استعلام کنید.");
        var result = await client.GetWorkFolderAsync(submission, ct);
        if (result is null)
        {
            await repository.RecordInquiryWithoutResultAsync(request.DispatchId, attempt, TaxDispatchEventKind.WorkFolder, ct);
            return new(null, null, null);
        }
        await repository.RecordWorkFolderAsync(request.DispatchId, attempt, result.Evidence, ct);
        return new(result.InvoiceStatus, result.Article6Status, result.Error);
    }
}

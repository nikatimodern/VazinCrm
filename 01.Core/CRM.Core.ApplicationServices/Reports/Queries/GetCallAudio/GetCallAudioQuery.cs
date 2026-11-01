using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Reports.Repositories;
using CRM.Core.ApplicationServices.Reports.Abstractions;
using CRM.Core.ApplicationServices.Reports.Dtos;
namespace CRM.Core.ApplicationServices.Reports.Queries.GetCallAudio;
public sealed record GetCallAudioQuery(Guid CallId) : IQuery<GetCallAudioResult?>;
public sealed class GetCallAudioQueryHandler(IReportRepository repository, IReportCallAudio audio) : IQueryHandler<GetCallAudioQuery, GetCallAudioResult?>
{
    public async Task<GetCallAudioResult?> HandleAsync(GetCallAudioQuery request, CancellationToken cancellationToken)
    {
        var path = await repository.GetCallRecordingPathAsync(request.CallId, cancellationToken);
        if (path is null) return null;
        var stream = await audio.OpenAsync(path, cancellationToken);
        return stream is null ? null : new GetCallAudioResult(stream, Path.GetFileName(path));
    }
}

public sealed record GetCallAudioResult(Stream Stream, string FileName);

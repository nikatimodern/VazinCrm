using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlQuestions;

public sealed record GetQualityControlQuestionsQuery : IQuery<IReadOnlyList<QualityControlQuestionDto>>;

public sealed class GetQualityControlQuestionsQueryHandler(IQualityControlRepository repository, CurrentActor actor)
    : IQueryHandler<GetQualityControlQuestionsQuery, IReadOnlyList<QualityControlQuestionDto>>
{
    public async Task<IReadOnlyList<QualityControlQuestionDto>> HandleAsync(GetQualityControlQuestionsQuery query, CancellationToken ct)
    {
        QualityControlAccessPolicy.For(await actor.GetAsync(ct));
        return await repository.GetQuestionsAsync(ct);
    }
}

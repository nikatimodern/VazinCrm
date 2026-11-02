using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlQuestions;

public sealed record GetQualityControlQuestionsQuery : IQuery<IReadOnlyList<QualityControlQuestionDto>>;

public sealed class GetQualityControlQuestionsQueryHandler(IQualityControlRepository repository)
    : IQueryHandler<GetQualityControlQuestionsQuery, IReadOnlyList<QualityControlQuestionDto>>
{
    public Task<IReadOnlyList<QualityControlQuestionDto>> HandleAsync(GetQualityControlQuestionsQuery query, CancellationToken ct)
        => repository.GetQuestionsAsync(ct);
}

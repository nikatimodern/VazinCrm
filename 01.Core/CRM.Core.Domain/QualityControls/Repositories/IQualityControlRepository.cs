#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.QualityControls.Models;

namespace CRM.Core.Domain.QualityControls.Repositories;

public interface IQualityControlRepository
{
    Task<QualityControlSaveResult> MarkNoResponseAsync(MarkQualityControlNoResponseRequest request, QualityControlAccess access, string persianNow, CancellationToken cancellationToken);
    Task<QualityControlSaveResult> DeleteAsync(DeleteQualityControlRequest request, QualityControlAccess access, CancellationToken cancellationToken);
    Task<QualityControlListResult> GetDealsAsync(QualityControlListRequest request, QualityControlAccess access, CancellationToken cancellationToken);
    Task<IReadOnlyList<QualityControlQuestionDto>> GetQuestionsAsync(CancellationToken cancellationToken);
    Task<QualityControlForm?> GetFormAsync(Guid dealId, QualityControlAccess access, CancellationToken cancellationToken);
    Task<QualityControlSaveResult> SaveAsync(SaveQualityControlRequest request, QualityControlAccess access, string persianNow, CancellationToken cancellationToken);
}

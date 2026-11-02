using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.QualityControls.Services;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;

namespace CRM.Core.ApplicationServices.QualityControls.Commands.SaveQualityControl;

public sealed record SaveQualityControlCommand(SaveQualityControlRequest Request) : ICommand<QualityControlSaveResult>;

public sealed class SaveQualityControlCommandHandler(IQualityControlRepository repository, CurrentActor actor, IApplicationDates dates)
    : ICommandHandler<SaveQualityControlCommand, QualityControlSaveResult>
{
    public async Task<QualityControlSaveResult> HandleAsync(SaveQualityControlCommand command, CancellationToken ct)
    {
        var access = QualityControlAccessPolicy.For(await actor.GetAsync(ct));
        var input = command.Request;
        if (input.DealId == Guid.Empty)
            return new(400, "سفارش معتبر نیست.");
        if (input.Description?.Length > 4000)
            return new(400, "توضیحات نباید بیشتر از ۴۰۰۰ نویسه باشد.");
        if (input.Answers == null || input.Answers.Count == 0
            || input.Answers.Any(a => a == null || a.QuestionId == Guid.Empty || a.Answer is not (0 or 1))
            || input.Answers.Select(a => a.QuestionId).Distinct().Count() != input.Answers.Count)
            return new(400, "پاسخ همهٔ سؤال‌ها باید انتخاب شود و فقط صفر یا یک باشد.");
        return await repository.SaveAsync(input, access, dates.PersianNow(), ct);
    }
}

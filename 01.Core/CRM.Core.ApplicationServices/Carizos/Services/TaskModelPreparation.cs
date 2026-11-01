using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Framework;

namespace CRM.Core.ApplicationServices.Carizos.Services;
public static class TaskModelPreparation
{
    public static async Task<Carizo> PrepareAsync(Carizo model, AppUser _user, ICarizoRepository repository, IApplicationDates dates, CancellationToken ct)
    {
        if (model.Repeat == true && model.Remind == true)
        {
            if (model.Date == string.Empty)
            {
                model.Date = dates.PersianToday();
                model.DateMiladi = DateTime.Now;
            }

            TimeSpan time = TimeSpan.Parse(BaseServices.ToEnglishNumber(model.RemindTime));
            DateTime? d = dates.ParsePersian(model.Date);
            model.DateMiladi = d.Value.Date.Add(time);
        }
        else if (model.Repeat == true)
        {
            if (model.Date == string.Empty)
            {
                model.Date = dates.PersianToday();
                model.DateMiladi = DateTime.Now.Date;
            }
            else
            {
                model.DateMiladi = dates.ParsePersian(BaseServices.ToEnglishNumber(model.Date));
            }
        }
        else if (model.Remind == true)
        {
            if (model.Date == string.Empty)
            {
                model.Date = dates.PersianToday();
            }

            TimeSpan t = TimeSpan.Parse(BaseServices.ToEnglishNumber(model.RemindTime));
            DateTime? d = dates.ParsePersian(BaseServices.ToEnglishNumber(model.Date));
            model.DateMiladi = d.Value.Date.Add(t);
        }
        else
        {
            if (model.Date != string.Empty)
                model.DateMiladi = dates.ParsePersian(model.Date);
        }

        switch (model.RepeatType)
        {
            case "daily":
                model.RepeatTypeTxt = "هرروز";
                break;
            case "weekly":
                model.RepeatTypeTxt = "هرهفته";
                break;
            case "monthly":
                model.RepeatTypeTxt = "هرماه";
                break;
            case "custom":
                model.RepeatTypeTxt = "سفارشی";
                break;
        }

        if (model.Id == Guid.Empty)
        {
            model.UserCreator = _user.FullName;
            model.UserCreatorID = _user.Id;
            model.AssignUser = _user.FullName;
            model.AssignUserId = _user.Id;
            model.ShareTaskId = model.Id;
        }
        else
        {
            var preTaskUp = await repository.GetAsync(model.Id, false, ct);
            model.UserCreator = preTaskUp.UserCreator;
            model.UserCreatorID = preTaskUp.UserCreatorID;
            model.AssignUser = preTaskUp.AssignUser;
            model.AssignUserId = preTaskUp.AssignUserId;
            model.ShareTaskId = preTaskUp.ShareTaskId;
        }

        model.ShareTaskId = model.Id;
        return model;
    }
}

using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Enum;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.ApplicationServices.Carizos.Abstractions;
using CRM.Core.ApplicationServices.Carizos.Services;

namespace CRM.Core.ApplicationServices.Carizos.Commands.SaveTask;
public sealed record SaveTaskCommand(Carizo Task) : ICommand<object>;
public sealed class SaveTaskCommandHandler(ICarizoRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit, ICarizoEffects effects) : ICommandHandler<SaveTaskCommand, object>
{
    public async Task<object> HandleAsync(SaveTaskCommand request, CancellationToken ct)
    {
        var _model = request.Task;
        try
        {
            var _user = (await actor.GetAsync(ct)).User;
            if (_model.EntityType == "Deal")
                await repository.TouchDealAsync(_model.EntityId, DateTime.Now, dates.PersianNow(), ct);
            var _GenModel = await TaskModelPreparation.PrepareAsync(_model, _user, repository, dates, ct);
            if (_model.Id == Guid.Empty)
            {
                _model.Id = Guid.NewGuid();
                switch (_model.TaskType)
                {
                    case TaskTypeEnum.تکی:
                        repository.Add(_GenModel);
                        await audit.LogAsync(_GenModel, null, LogActionEnum.ثبت, _user, ct);
                        break;
                    default:
                        var model2 = _GenModel;
                        model2.Id = Guid.NewGuid();
                        model2.ShareTaskId = model2.Id;
                        repository.Add(model2);
                        IList<Carizo> list = new List<Carizo>();
                        if (list == null)
                            throw new ArgumentNullException(nameof(list));
                        foreach (var item in model2.TaskUsers)
                        {
                            Carizo CModel = model2.Clone();
                            CModel.AssignUser = item.FullName;
                            CModel.AssignUserId = item.UserId.ToString();
                            CModel.TaskUsers = null;
                            CModel.ShareTaskId = model2.Id;
                            CModel.UserCreator = model2.UserCreator;
                            CModel.UserCreatorID = model2.UserCreatorID;
                            list.Add(CModel);
                            await effects.NotifyAsync(CModel.AssignUserId, $"فعالیت <b>{CModel.Title}</b> توسط <b>{_user.FullName}</b> برای شما ثبت شد", ct);
                            effects.Push(CModel.AssignUserId, "کاریزوی جدید", $"یک فعالیت جدید توسط {_user.FirstName} در کاریزوی شما ثبت شد.", "carizo-assign" + model2.Id);
                        }

                        foreach (var task in list)
                            repository.Add(task);
                        await audit.LogAsync(model2, null, LogActionEnum.ثبت, _user, ct);
                        break;
                }
            }
            else
            {
                var oldModel = await repository.GetAsync(_model.Id, false, ct);
                if (_GenModel.UserCreatorID == _user.Id)
                {
                    switch (_model.TaskType)
                    {
                        case TaskTypeEnum.تکی:
                            var model1 = _GenModel;
                            repository.Update(model1);
                            break;
                        default:
                            var model2 = _GenModel;
                            foreach (var item in _GenModel.TaskUsers)
                            {
                                if (item.IsDeleted)
                                {
                                    var dTask = await repository.GetAssignedAsync(_GenModel.Id, item.UserId.ToString(), ct);
                                    dTask.IsDeleted = true;
                                    repository.Remove(dTask);
                                    await effects.NotifyAsync(item.UserId.ToString(), $"کار <b>{dTask.Title}</b> توسط <b>{_user.FullName}</b> حذف شد", ct);
                                }
                                else if (item.Id == Guid.Empty)
                                {
                                    Carizo CModel = model2.Clone();
                                    CModel.AssignUser = item.FullName;
                                    CModel.AssignUserId = item.UserId.ToString();
                                    CModel.TaskUsers = null;
                                    CModel.ShareTaskId = model2.Id;
                                    CModel.UserCreator = model2.UserCreator;
                                    CModel.UserCreatorID = model2.UserCreatorID;
                                    repository.Add(CModel);
                                    await effects.NotifyAsync(CModel.AssignUserId, $"کار <b>{CModel.Title}</b> توسط <b>{_user.FullName}</b> برای شما ثبت شد", ct);
                                    effects.Push(CModel.AssignUserId, "کاریزوی جدید", $"یک فعالیت جدید توسط {_user.FirstName} در کاریزوی شما ثبت شد.", "carizo-assign" + model2.Id);
                                }
                                else
                                {
                                    var dTask = await repository.GetAssignedAsync(_GenModel.Id, item.UserId.ToString(), ct);
                                    dTask.IsDeleted = true;
                                    Carizo CModel = model2.Clone();
                                    CModel.AssignUser = item.FullName;
                                    CModel.AssignUserId = item.UserId.ToString();
                                    CModel.TaskUsers = null;
                                    CModel.ShareTaskId = model2.Id;
                                    CModel.UserCreator = model2.UserCreator;
                                    CModel.UserCreatorID = model2.UserCreatorID;
                                    CModel.TaskStatus = dTask.TaskStatus;
                                    repository.Add(CModel);
                                    await effects.NotifyAsync(item.UserId.ToString(), $"کار <b>{CModel.Title}</b> توسط <b>{_user.FullName}</b> تغییر کرد", ct);
                                    effects.Push(CModel.AssignUserId, "تغییر کاربان", $"فعالیت {CModel.Title} توسط {_user.FullName} تغییر کرد", "carizo-assign" + model2.Id);
                                }
                            }

                            if (!model2.TaskUsers.Where(f => f.IsDeleted != true).Any())
                            {
                                model2.TaskType = TaskTypeEnum.تکی;
                                model2.TaskTypeTxt = TaskTypeEnum.تکی.ToString();
                            }

                            repository.Update(model2);
                            break;
                    }
                }
                else
                {
                    repository.Update(_GenModel);
                }

                await audit.LogAsync(oldModel, _GenModel, LogActionEnum.ویرایش, _user, ct);
            }

            await repository.SaveAsync(ct);
            await effects.RefreshAsync(ct, _model.Id);
            return new
            {
                status = "200",
                error = "",
                data = ""
            };
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            return new
            {
                Status = "500",
                error = e.Message,
                data = ""
            };
        }
    }
}

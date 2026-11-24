using CRM.Core.Domain.AccessCatalog.Repositories;
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
public sealed class SaveTaskCommandHandler(ICarizoRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit, ICarizoEffects effects, IDocumentReadAccess access) : ICommandHandler<SaveTaskCommand, object>
{
    public async Task<object> HandleAsync(SaveTaskCommand request, CancellationToken ct)
    {
        var _model = request.Task;
        try
        {
            var _user = (await actor.GetAsync(ct)).User;
            if (_model == null) return 400;
            if (access.IsEnforced("Carizo"))
            {
                if (_model.Id == Guid.Empty)
                {
                    if (!await access.HasAsync("Carizo", "Create", ct))
                        return 403;
                    if (!Enum.IsDefined(_model.TaskType)) return 400;
                    if (_model.TaskUsers?.Any(u => u.Id != Guid.Empty || u.IsDeleted) == true) return 400;
                    _model.IsDeleted = false;
                    _model.Systematic = false;
                    _model.TaskStatus = false;
                    _model.TaskHide = false;
                    _model.CompleteTaskUser = null;
                    _model.CompleteTaskUserId = null;
                    if (_model.EntityId.HasValue && _model.EntityType is not ("Deal" or "Report"))
                        return 403;
                }
                else
                {
                    if (!await repository.CanAccessAsync(_model.Id, "Edit", ct))
                        return 403;
                    var original = await repository.GetAsync(_model.Id, false, ct);
                    if (original == null) return 404;
                    if (original.TaskType != TaskTypeEnum.تکی && original.UserCreatorID == _user.Id &&
                        !await repository.CanMutateSharedAsync(original.ShareTaskId ?? original.Id, "Edit", ct))
                        return 403;
                    if (original.UserCreatorID != _user.Id &&
                        (_model.TaskUsers?.Count > 0 || _model.TaskType != original.TaskType))
                        return 403;
                    _model.IsDeleted = original.IsDeleted;
                    _model.Systematic = original.Systematic;
                    _model.EntityType = original.EntityType;
                    _model.EntityId = original.EntityId;
                    _model.TaskStatus = original.TaskStatus;
                    _model.TaskHide = original.TaskHide;
                    _model.CompleteTaskUser = original.CompleteTaskUser;
                    _model.CompleteTaskUserId = original.CompleteTaskUserId;
                    _model.CreateDate = original.CreateDate;
                    _model.CreateDateTxt = original.CreateDateTxt;
                    if (original.Systematic || original.UserCreatorID != _user.Id)
                    {
                        _model.Title = original.Title;
                        _model.Description = original.Description;
                        _model.TaskType = original.TaskType;
                        _model.TaskTypeTxt = original.TaskTypeTxt;
                    }
                    if (original.TaskUsers != null && _model.TaskUsers != null)
                        foreach (var participant in _model.TaskUsers)
                        {
                            var stored = original.TaskUsers.FirstOrDefault(x => x.Id == participant.Id && x.UserId == participant.UserId);
                            participant.completed = stored?.completed ?? false;
                            participant.TaskId = original.Id;
                        }
                    if (_model.TaskUsers != null && _model.TaskUsers.Any(u => u.Id != Guid.Empty &&
                        !(original.TaskUsers ?? new List<TaskUser>()).Any(x => x.Id == u.Id && x.UserId == u.UserId)))
                        return 403;
                }
                if (_model.TaskUsers?.Count > 0)
                {
                    var ids = _model.TaskUsers.Select(x => x.UserId).ToArray();
                    if (ids.Any(x => x == Guid.Empty) || ids.Distinct().Count() != ids.Length) return 400;
                    var names = await repository.ResolveParticipantsAsync(ids, ct);
                    if (names.Count != ids.Length) return 403;
                    foreach (var participant in _model.TaskUsers)
                    {
                        participant.FullName = names[participant.UserId];
                        if (participant.Id == Guid.Empty) participant.completed = false;
                    }
                }
            }
            if (_model.EntityType == "Deal" && !access.IsEnforced("Carizo"))
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
            return 500;
        }
    }
}

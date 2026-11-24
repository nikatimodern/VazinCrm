#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Carizos.Entities;

namespace CRM.Core.Domain.Carizos.Repositories;
public sealed record TaskCardChanges(Guid GroupId, List<Carizo> Tasks);
public interface ICarizoRepository
{
    Task<bool> CanAccessAsync(Guid id, string action, CancellationToken ct);
    Task<bool> CanMutateSharedAsync(Guid? shareId, string action, CancellationToken ct);
    Task<bool> CanPropagateStatusAsync(Guid shareId, string actorId, CancellationToken ct);
    Task<Dictionary<Guid, string>> ResolveParticipantsAsync(Guid[] userIds, CancellationToken ct);
    Task<Carizo?> GetAsync(Guid? id, bool tracking, CancellationToken ct);
    Task<List<Carizo>> GetSharedAsync(Guid? shareId, Guid? exceptId, CancellationToken ct);
    Task<Carizo?> GetAssignedAsync(Guid? shareId, string userId, CancellationToken ct);
    Task<List<Carizo>> GetRemindersAsync(string userId, DateTime now, CancellationToken ct);
    Task<List<Carizo>> GetTasksAsync(string userId, int type, int systematic, DateTime now, CancellationToken ct);
    Task<TaskCardChanges> GetCardChangesAsync(Guid id, string userId, CancellationToken ct);
    void Add(Carizo task);
    void Update(Carizo task);
    void Remove(Carizo task);
    Task TouchDealAsync(Guid? id, DateTime now, string date, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

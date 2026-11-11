#nullable enable
using System.Collections.Generic;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.ReleaseNotes.Dtos;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Repositories;
using CRM.Infrastructures.Sql.ReleaseNotes.Repositories;
using Microsoft.Extensions.DependencyInjection;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.CreateReleaseNote;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.UpdateReleaseNote;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.PublishReleaseNote;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.ArchiveReleaseNote;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.DeleteReleaseNoteDraft;
using CRM.Core.ApplicationServices.ReleaseNotes.Commands.AcknowledgeReleaseNotes;
using CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetUnseenReleaseNotes;
using CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNoteHistory;
using CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNote;
using CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNotes;
using CRM.Core.ApplicationServices.ReleaseNotes.Queries.GetReleaseNoteAdminPage;
namespace CRM.EndPoint.FrontEnd.Composition;
public static class ReleaseNotesRegistration
{
    public static IServiceCollection AddReleaseNotes(this IServiceCollection services)
    {
        services.AddScoped<IReleaseNoteRepository, ReleaseNoteRepository>();
        services.AddScoped<ReleaseNoteAccess>();
        services.AddScoped<ReleaseNoteAdministration>();
        services.AddScoped<ICommandHandler<CreateReleaseNoteCommand, ReleaseNoteResult>, CreateReleaseNoteCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateReleaseNoteCommand, ReleaseNoteResult>, UpdateReleaseNoteCommandHandler>();
        services.AddScoped<ICommandHandler<PublishReleaseNoteCommand, ReleaseNoteResult>, PublishReleaseNoteCommandHandler>();
        services.AddScoped<ICommandHandler<ArchiveReleaseNoteCommand, ReleaseNoteResult>, ArchiveReleaseNoteCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteReleaseNoteDraftCommand, ReleaseNoteResult>, DeleteReleaseNoteDraftCommandHandler>();
        services.AddScoped<ICommandHandler<AcknowledgeReleaseNotesCommand, ReleaseNoteResult>, AcknowledgeReleaseNotesCommandHandler>();
        services.AddScoped<IQueryHandler<GetUnseenReleaseNotesQuery, List<ReleaseNoteData>>, GetUnseenReleaseNotesQueryHandler>();
        services.AddScoped<IQueryHandler<GetReleaseNoteHistoryQuery, List<ReleaseNoteData>>, GetReleaseNoteHistoryQueryHandler>();
        services.AddScoped<IQueryHandler<GetReleaseNoteQuery, ReleaseNoteData?>, GetReleaseNoteQueryHandler>();
        services.AddScoped<IQueryHandler<GetReleaseNotesQuery, List<ReleaseNoteData>>, GetReleaseNotesQueryHandler>();
        services.AddScoped<IQueryHandler<GetReleaseNoteAdminPageQuery, ReleaseNoteAdminPage>, GetReleaseNoteAdminPageQueryHandler>();
        return services;
    }
}

using System.Collections.Generic;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.RequestAmounts.Abstractions;
using CRM.Core.ApplicationServices.RequestAmounts.Services;
using CRM.Core.ApplicationServices.RequestAmounts.Dtos;
using CRM.Core.Domain.RequestAmounts.Repositories;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.EndPoint.FrontEnd.Services.RequestAmounts;
using CRM.Infrastructures.Sql.RequestAmounts.Repositories;
using Microsoft.Extensions.DependencyInjection;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.AddRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.UpdateRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.ChangeDocTypeRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.SetAcceptedRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.ChangeStatusRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.AddPaymentByRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.DeletePayment;
using CRM.Core.ApplicationServices.RequestAmounts.Commands.DeleteRequestAmount;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestAmountDetails;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestAmountList;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestsByPayment;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetSelectedRequests;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestPaymentsTable;
using CRM.Core.ApplicationServices.RequestAmounts.Queries.GetRequestAmountsTable;

namespace CRM.EndPoint.FrontEnd.Composition;
public static class RequestAmountServicesRegistration
{
    public static IServiceCollection AddRequestAmountServices(this IServiceCollection services)
    {
        services.AddScoped<IRequestAmountRepository, RequestAmountRepository>();
        services.AddScoped<IRequestAmountEffects, RequestAmountEffects>();
        services.AddScoped<RequestAmountWorkflow>();
        services.AddScoped<ICommandHandler<AddRequestAmountCommand, RequestAmountOutcome>, AddRequestAmountCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateRequestAmountCommand, RequestAmountOutcome>, UpdateRequestAmountCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeDocTypeRequestAmountCommand, RequestAmountOutcome>, ChangeDocTypeRequestAmountCommandHandler>();
        services.AddScoped<ICommandHandler<SetAcceptedRequestAmountCommand, RequestAmountOutcome>, SetAcceptedRequestAmountCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeStatusRequestAmountCommand, RequestAmountOutcome>, ChangeStatusRequestAmountCommandHandler>();
        services.AddScoped<ICommandHandler<AddPaymentByRequestAmountCommand, RequestAmountOutcome>, AddPaymentByRequestAmountCommandHandler>();
        services.AddScoped<ICommandHandler<DeletePaymentCommand, RequestAmountOutcome>, DeletePaymentCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteRequestAmountCommand, RequestAmountOutcome>, DeleteRequestAmountCommandHandler>();
        services.AddScoped<IQueryHandler<GetRequestAmountDetailsQuery, RequestAmountRow?>, GetRequestAmountDetailsQueryHandler>();
        services.AddScoped<IQueryHandler<GetRequestAmountListQuery, object>, GetRequestAmountListQueryHandler>();
        services.AddScoped<IQueryHandler<GetRequestsByPaymentQuery, List<RequestAmount>>, GetRequestsByPaymentQueryHandler>();
        services.AddScoped<IQueryHandler<GetSelectedRequestsQuery, SelectedRequests>, GetSelectedRequestsQueryHandler>();
        services.AddScoped<IQueryHandler<GetRequestPaymentsTableQuery, object>, GetRequestPaymentsTableQueryHandler>();
        services.AddScoped<IQueryHandler<GetRequestAmountsTableQuery, RequestAmountsTable>, GetRequestAmountsTableQueryHandler>();
        return services;
    }
}

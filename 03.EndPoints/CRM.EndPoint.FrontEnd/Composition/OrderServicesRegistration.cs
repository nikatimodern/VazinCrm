using System;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Orders.Abstractions;
using CRM.Core.Domain.Orders.Repositories;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.EndPoint.FrontEnd.Services.Orders;
using CRM.Infrastructures.Sql.Orders.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderHeadsByPreFactor;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderPreFactorOptions;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderFactorOptions;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderItem;
using CRM.Core.ApplicationServices.Orders.Queries.GetMissingOrderItems;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderPrint;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderHeadsTable;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderCustomerOptions;
using CRM.Core.ApplicationServices.Orders.Queries.GetVisibleOrderHeadIds;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderItemsTable;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderHeadDetails;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderPreFactorHeads;
using CRM.Core.ApplicationServices.Orders.Queries.SelectOrderHeads;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderBoard;
using CRM.Core.ApplicationServices.Orders.Queries.GetDealOrderBoard;
using CRM.Core.ApplicationServices.Orders.Queries.GetOrderDocument;
using CRM.Core.ApplicationServices.Orders.Commands.UpdateOrderHeadDescription;
using CRM.Core.ApplicationServices.Orders.Commands.CreateOrderItem;
using CRM.Core.ApplicationServices.Orders.Commands.UpdateOrderItem;
using CRM.Core.ApplicationServices.Orders.Commands.DeleteOrderItem;
using CRM.Core.ApplicationServices.Orders.Commands.DeleteOrderHead;
using CRM.Core.ApplicationServices.Orders.Commands.ChangeOrderHeadStatus;
using CRM.Core.ApplicationServices.Orders.Commands.UpdateOrderHead;
using CRM.Core.ApplicationServices.Orders.Commands.SaveOrderHeads;

namespace CRM.EndPoint.FrontEnd.Composition;
public static class OrderServicesRegistration
{
    public static IServiceCollection AddOrderServices(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderEffects, OrderEffects>();
        services.AddScoped<IQueryHandler<GetOrderHeadsByPreFactorQuery, List<OrderHead>>, GetOrderHeadsByPreFactorQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderPreFactorOptionsQuery, List<OrderOption>>, GetOrderPreFactorOptionsQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderFactorOptionsQuery, List<OrderOption>>, GetOrderFactorOptionsQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderItemQuery, Order?>, GetOrderItemQueryHandler>();
        services.AddScoped<IQueryHandler<GetMissingOrderItemsQuery, List<PreFactorItem>>, GetMissingOrderItemsQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderPrintQuery, OrderPrintData>, GetOrderPrintQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderHeadsTableQuery, OrderHeadTableResult>, GetOrderHeadsTableQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderCustomerOptionsQuery, List<OrderOption>>, GetOrderCustomerOptionsQueryHandler>();
        services.AddScoped<IQueryHandler<GetVisibleOrderHeadIdsQuery, List<Guid>>, GetVisibleOrderHeadIdsQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderItemsTableQuery, OrderTableResult>, GetOrderItemsTableQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderHeadDetailsQuery, OrderHeadDetails>, GetOrderHeadDetailsQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderPreFactorHeadsQuery, PreFactorOrderHeads>, GetOrderPreFactorHeadsQueryHandler>();
        services.AddScoped<IQueryHandler<SelectOrderHeadsQuery, OrderHeadSelection>, SelectOrderHeadsQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderBoardQuery, object>, GetOrderBoardQueryHandler>();
        services.AddScoped<IQueryHandler<GetDealOrderBoardQuery, object>, GetDealOrderBoardQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderDocumentQuery, byte[]>, GetOrderDocumentQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateOrderHeadDescriptionCommand, int>, UpdateOrderHeadDescriptionCommandHandler>();
        services.AddScoped<ICommandHandler<CreateOrderItemCommand, int>, CreateOrderItemCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateOrderItemCommand, int>, UpdateOrderItemCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteOrderItemCommand, int>, DeleteOrderItemCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteOrderHeadCommand, int>, DeleteOrderHeadCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeOrderHeadStatusCommand, int>, ChangeOrderHeadStatusCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateOrderHeadCommand, int>, UpdateOrderHeadCommandHandler>();
        services.AddScoped<ICommandHandler<SaveOrderHeadsCommand, int>, SaveOrderHeadsCommandHandler>();
        return services;
    }
}

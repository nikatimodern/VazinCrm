using CRM.Core.Domain.Deals.Repositories;
using CRM.Infrastructures.Sql.Deals.Repositories;
using CRM.Core.ApplicationServices.OverdueDeals.Queries.GetOverdueDeals;
using CRM.Core.ApplicationServices.OverdueDeals.Queries.GetOverdueDealDetails;
using CRM.Core.ApplicationServices.OverdueDeals.Commands.UpdateOverdueFollowUp;
using CRM.Core.Domain.Transfers.Repositories;
using CRM.Infrastructures.Sql.Transfers.Repositories;
using CRM.Core.ApplicationServices.Transfers.Abstractions;
using CRM.EndPoint.FrontEnd.Services.Transfers;
using CRM.Core.ApplicationServices.Transfers.Queries.GetTransfersTable;
using CRM.Core.ApplicationServices.Transfers.Queries.GetTransfer;
using CRM.Core.ApplicationServices.Transfers.Commands.SaveTransfer;
using CRM.Core.ApplicationServices.Transfers.Commands.DeleteTransfer;
using CRM.Core.ApplicationServices.Transfers.Commands.ChangeTransferStatus;
using System.Collections.Generic;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.Products.Repositories;
using CRM.Infrastructures.Sql.Products.Repositories;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Repositories;
using CRM.Infrastructures.Sql.Checks.Repositories;
using Microsoft.Extensions.DependencyInjection;
using CRM.Core.ApplicationServices.Amounts.Queries.GetProductPrices;
using CRM.Core.ApplicationServices.Amounts.Commands.SaveProductPrice;
using CRM.Core.ApplicationServices.Amounts.Commands.DeleteProductPrice;
using CRM.Core.ApplicationServices.Checks.Queries.GetCheckbooks;
using CRM.Core.ApplicationServices.Checks.Queries.GetChecksByCheckbook;
using CRM.Core.ApplicationServices.Checks.Queries.GetCheck;
using CRM.Core.ApplicationServices.Checks.Commands.CreateCheckbook;
using CRM.Core.ApplicationServices.Checks.Commands.SaveCheck;
using CRM.Core.ApplicationServices.Checks.Commands.ChangeCheckStatus;
using CRM.Core.ApplicationServices.Checks.Commands.DeleteCheck;
using CRM.Core.ApplicationServices.Checks.Commands.DeleteCheckbook;

namespace CRM.EndPoint.FrontEnd.Composition;
public static class PageModulesRegistration
{
    public static IServiceCollection AddPageModules(this IServiceCollection services)
    {
        services.AddScoped<IProductPriceRepository, ProductPriceRepository>();
        services.AddScoped<ICheckRepository, CheckRepository>();
        services.AddScoped<IQueryHandler<GetProductPricesQuery, ProductPricesResult>, GetProductPricesQueryHandler>();
        services.AddScoped<ICommandHandler<SaveProductPriceCommand, int>, SaveProductPriceCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteProductPriceCommand, int>, DeleteProductPriceCommandHandler>();
        services.AddScoped<IQueryHandler<GetCheckbooksQuery, List<CheckbookInfo>>, GetCheckbooksQueryHandler>();
        services.AddScoped<IQueryHandler<GetChecksByCheckbookQuery, CheckListResult>, GetChecksByCheckbookQueryHandler>();
        services.AddScoped<IQueryHandler<GetCheckQuery, Check?>, GetCheckQueryHandler>();
        services.AddScoped<ICommandHandler<CreateCheckbookCommand, int>, CreateCheckbookCommandHandler>();
        services.AddScoped<ICommandHandler<SaveCheckCommand, int>, SaveCheckCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeCheckStatusCommand, int>, ChangeCheckStatusCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteCheckCommand, int>, DeleteCheckCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteCheckbookCommand, int>, DeleteCheckbookCommandHandler>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<ITransferEffects, TransferEffects>();
        services.AddScoped<IQueryHandler<GetTransfersTableQuery, TransferTableResult>, GetTransfersTableQueryHandler>();
        services.AddScoped<IQueryHandler<GetTransferQuery, TransferDetails>, GetTransferQueryHandler>();
        services.AddScoped<ICommandHandler<SaveTransferCommand, int>, SaveTransferCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteTransferCommand, int>, DeleteTransferCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeTransferStatusCommand, int>, ChangeTransferStatusCommandHandler>();
        services.AddScoped<IOverdueDealRepository, OverdueDealRepository>();
        services.AddScoped<IQueryHandler<GetOverdueDealsQuery, OverdueDealsResult>, GetOverdueDealsQueryHandler>();
        services.AddScoped<IQueryHandler<GetOverdueDealDetailsQuery, OverdueDealDetails>, GetOverdueDealDetailsQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateOverdueFollowUpCommand, FollowUpResult>, UpdateOverdueFollowUpCommandHandler>();
        return services;
    }
}

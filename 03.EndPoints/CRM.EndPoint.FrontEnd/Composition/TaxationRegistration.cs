using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxInvoice;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxPreviewSnapshot;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Taxation;
using CRM.Infrastructures.Sql.Taxation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemory;
using CRM.Core.ApplicationServices.Taxation.Commands.ActivateTaxMemory;
using CRM.Core.ApplicationServices.Taxation.Queries.ListTaxMemories;
using CRM.Core.ApplicationServices.Taxation.Queries.PreviewTaxInvoice;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxPreview;
using CRM.Core.ApplicationServices.Taxation.Commands.SetTaxProductMapping;

namespace CRM.EndPoint.FrontEnd.Composition;

internal static class TaxationRegistration
{
    internal static void AddTaxation(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetTaxPreviewSnapshotQuery, InvoicePreview>, GetTaxPreviewSnapshotQueryHandler>();
        services.AddScoped<ITaxPreviewProtector, TaxPreviewProtector>();
        services.AddScoped<ITaxDispatchQueue, TaxDispatchQueue>();
        services.AddScoped<ICommandHandler<EnqueueTaxInvoiceCommand, Guid>, EnqueueTaxInvoiceCommandHandler>();
        services.AddScoped<ITaxationRepository, TaxationRepository>();
        services.AddScoped<ICommandHandler<SaveTaxMemoryCommand, Guid>, SaveTaxMemoryCommandHandler>();
        services.AddScoped<ICommandHandler<ActivateTaxMemoryCommand, bool>, ActivateTaxMemoryCommandHandler>();
        services.AddScoped<IQueryHandler<ListTaxMemoriesQuery, IReadOnlyList<TaxMemory>>, ListTaxMemoriesQueryHandler>();
        services.AddScoped<IQueryHandler<PreviewTaxInvoiceQuery, InvoicePreview>, PreviewTaxInvoiceQueryHandler>();
        services.AddScoped<ICommandHandler<SaveTaxPreviewCommand, Guid>, SaveTaxPreviewCommandHandler>();
        services.AddScoped<ICommandHandler<SetTaxProductMappingCommand, bool>, SetTaxProductMappingCommandHandler>();
    }
}

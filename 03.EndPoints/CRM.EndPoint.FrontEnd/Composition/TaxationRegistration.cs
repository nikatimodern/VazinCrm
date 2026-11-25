using CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxPayment;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxConnection;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxPayment;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxPayment;
using CRM.Core.ApplicationServices.Taxation.Queries.CheckTaxV1Connection;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxConnectionCheck;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemorySettings;
using CRM.Core.ApplicationServices.Taxation.Queries.ListTaxInvoices;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxDispatch;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxInvoiceBatch;
using CRM.Core.ApplicationServices.Taxation.Commands.DeactivateTaxMemory;
using CRM.Core.ApplicationServices.Taxation.Queries.ListTaxProducts;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxReport;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxReference;
using CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxInvoice;
using CRM.Core.ApplicationServices.Taxation.Commands.InquireTaxSubmission;
using CRM.Core.ApplicationServices.Taxation.Commands.InquireTaxWorkFolder;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.EndPoint.FrontEnd.Integrations.Moadian;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
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
        services.AddScoped<TaxationAuditFilter>();
        services.AddScoped<IQueryHandler<GetTaxPreviewSnapshotQuery, InvoicePreview>, GetTaxPreviewSnapshotQueryHandler>();
        services.AddScoped<ITaxPreviewProtector, TaxPreviewProtector>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITaxCertificateStore, TaxCertificateStore>();
        services.AddScoped<ITaxSigningBindings, SandboxTaxSigningBindings>();
        services.AddScoped<ITaxMemoryReadiness, TaxMemoryReadiness>();
        services.AddScoped<ITaxPaymentRepository, TaxPaymentRepository>();
        services.AddScoped<ICommandHandler<EnqueueTaxPaymentCommand, Guid>, EnqueueTaxPaymentCommandHandler>();
        services.AddScoped<IQueryHandler<GetTaxPaymentQuery, TaxPaymentState>, GetTaxPaymentQueryHandler>();
        services.AddScoped<ITaxSubmissionRepository, TaxSubmissionRepository>();
        services.AddSingleton<TaxConnectionSettings>();
        services.AddSingleton<ITaxConnectionSettings>(sp => sp.GetRequiredService<TaxConnectionSettings>());
        services.AddSingleton<ITaxV1SigningKeys>(sp => sp.GetRequiredService<TaxConnectionSettings>());
        services.AddScoped<ICommandHandler<SaveTaxConnectionCommand, bool>, SaveTaxConnectionCommandHandler>();
        services.AddScoped<IQueryHandler<CheckTaxV1ConnectionQuery, bool>, CheckTaxV1ConnectionQueryHandler>();
        services.AddSingleton(sp => CreateProtocolClients(sp, productionCheck: false));
        services.AddKeyedSingleton(TaxProductionConnectionHandler.ClientKey,
            (sp, _) => CreateProtocolClients(sp, productionCheck: true));
        services.AddSingleton<ITaxV1IdentityProbe>(sp => new TaxV1IdentityProbe(sp.GetRequiredService<TaxProtocolClients>().V1));
        services.AddScoped<TaxSandboxRouting>();
        services.AddScoped<ITaxSandboxAccess>(sp => sp.GetRequiredService<TaxSandboxRouting>());
        services.AddScoped<ITaxAuthorityClient, TaxAuthorityRouter>();
        services.AddScoped<ITaxPaymentTransport, TaxPaymentTransport>();
        services.AddSingleton<ITaxMemoryCredentials, TaxMemoryCredentials>();
        services.AddScoped<ITaxConnectionCheck, TaxConnectionCheck>();
        services.AddScoped<ICommandHandler<SaveTaxMemorySettingsCommand, Guid>, SaveTaxMemorySettingsCommandHandler>();
        services.AddScoped<IQueryHandler<GetTaxConnectionCheckQuery, IReadOnlyList<TaxConnectionStep>>, GetTaxConnectionCheckQueryHandler>();
        services.AddScoped<ICommandHandler<SubmitTaxPaymentCommand, TaxPaymentState>, SubmitTaxPaymentCommandHandler>();
        services.AddScoped<ICommandHandler<SubmitTaxInvoiceCommand, TaxDispatchState>, SubmitTaxInvoiceCommandHandler>();
        services.AddScoped<ICommandHandler<InquireTaxSubmissionCommand, TaxDispatchState>, InquireTaxSubmissionCommandHandler>();
        services.AddScoped<ICommandHandler<InquireTaxWorkFolderCommand, TaxWorkFolderState>, InquireTaxWorkFolderCommandHandler>();
        services.AddScoped<ITaxDispatchQueue, TaxDispatchQueue>();
        services.AddScoped<ICommandHandler<EnqueueTaxReferenceCommand, Guid>, EnqueueTaxReferenceCommandHandler>();
        services.AddScoped<ICommandHandler<EnqueueTaxInvoiceCommand, Guid>, EnqueueTaxInvoiceCommandHandler>();
        services.AddScoped<ICommandHandler<EnqueueTaxInvoiceBatchCommand, IReadOnlyList<TaxInvoiceBatchResult>>,
            EnqueueTaxInvoiceBatchCommandHandler>();
        services.AddScoped<TaxationRepository>();
        services.AddScoped<ITaxationRepository>(sp => sp.GetRequiredService<TaxationRepository>());
        services.AddScoped<ITaxMemorySettingsRepository>(sp => sp.GetRequiredService<TaxationRepository>());
        services.AddScoped<ITaxInvoiceListing, TaxInvoiceListing>();
        services.AddScoped<ITaxReportRepository, TaxReportRepository>();
        services.AddScoped<IQueryHandler<GetTaxReportQuery, TaxReportData>, GetTaxReportQueryHandler>();
        services.AddScoped<IQueryHandler<ListTaxInvoicesQuery, TaxInvoicePage>, ListTaxInvoicesQueryHandler>();
        services.AddScoped<IQueryHandler<GetTaxDispatchQuery, TaxDispatchDetails>, GetTaxDispatchQueryHandler>();
        services.AddScoped<ICommandHandler<SaveTaxMemoryCommand, Guid>, SaveTaxMemoryCommandHandler>();
        services.AddScoped<ICommandHandler<ActivateTaxMemoryCommand, bool>, ActivateTaxMemoryCommandHandler>();
        services.AddScoped<ICommandHandler<DeactivateTaxMemoryCommand, bool>, DeactivateTaxMemoryCommandHandler>();
        services.AddScoped<IQueryHandler<ListTaxProductsQuery, TaxProductPage>, ListTaxProductsQueryHandler>();
        services.AddScoped<IQueryHandler<ListTaxMemoriesQuery, IReadOnlyList<TaxMemory>>, ListTaxMemoriesQueryHandler>();
        services.AddScoped<IQueryHandler<PreviewTaxInvoiceQuery, InvoicePreview>, PreviewTaxInvoiceQueryHandler>();
        services.AddScoped<ICommandHandler<SaveTaxPreviewCommand, Guid>, SaveTaxPreviewCommandHandler>();
        services.AddScoped<ICommandHandler<SetTaxProductMappingCommand, bool>, SetTaxProductMappingCommandHandler>();
    }

    private static TaxProtocolClients CreateProtocolClients(IServiceProvider services, bool productionCheck) =>
        new(CreateHttp(TaxAuthorityProtocol.V1, productionCheck), CreateHttp(TaxAuthorityProtocol.V2, productionCheck),
            services.GetRequiredService<ITaxV1SigningKeys>(), services.GetRequiredService<ITaxCertificateStore>(),
            services.GetRequiredService<TimeProvider>());

    private static HttpClient CreateHttp(TaxAuthorityProtocol protocol, bool productionCheck)
    {
        var transport = new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false };
        return new HttpClient(productionCheck ? new TaxProductionConnectionHandler(protocol, transport) : transport)
        {
            BaseAddress = productionCheck ? TaxProductionConnectionHandler.Address(protocol) : TaxSandboxRouting.Address(protocol),
            Timeout = TimeSpan.FromSeconds(20),
            MaxResponseContentBufferSize = 1024 * 1024
        };
    }
}

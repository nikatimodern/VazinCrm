using CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxPayment;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxPayment;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxPayment;
using CRM.Core.ApplicationServices.Taxation.Queries.ListTaxInvoices;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxReference;
using CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxInvoice;
using CRM.Core.ApplicationServices.Taxation.Commands.InquireTaxSubmission;
using CRM.Core.ApplicationServices.Taxation.Commands.InquireTaxWorkFolder;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.EndPoint.FrontEnd.Integrations.Moadian;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
        services.AddScoped<IQueryHandler<GetTaxPreviewSnapshotQuery, InvoicePreview>, GetTaxPreviewSnapshotQueryHandler>();
        services.AddScoped<ITaxPreviewProtector, TaxPreviewProtector>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITaxCertificateStore, TaxCertificateStore>();
        services.AddScoped<ITaxSigningBindings, SandboxTaxSigningBindings>();
        services.AddScoped<ITaxPaymentRepository, TaxPaymentRepository>();
        services.AddScoped<ICommandHandler<EnqueueTaxPaymentCommand, Guid>, EnqueueTaxPaymentCommandHandler>();
        services.AddScoped<IQueryHandler<GetTaxPaymentQuery, TaxPaymentState>, GetTaxPaymentQueryHandler>();
        services.AddScoped<ITaxSubmissionRepository, TaxSubmissionRepository>();
        services.AddSingleton<ITaxAuthorityClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var environment = sp.GetRequiredService<IWebHostEnvironment>();
            if (!environment.IsDevelopment() || !config.GetValue<bool>("Taxation:Sandbox:Enabled") ||
                !Uri.TryCreate(config["Taxation:Sandbox:BaseAddress"], UriKind.Absolute, out var address) ||
                address.Scheme != Uri.UriSchemeHttps || !address.IsDefaultPort ||
                address.Host != "sandboxrc.tax.gov.ir" || address.AbsolutePath != "/requestsmanager/api/v2/" ||
                address.UserInfo.Length != 0 || address.Query.Length != 0 || address.Fragment.Length != 0)
                throw new InvalidOperationException("Tax Sandbox transport is disabled or has an invalid address.");
            return new MoadianClient(new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false })
            {
                BaseAddress = address,
                Timeout = TimeSpan.FromSeconds(20),
                MaxResponseContentBufferSize = 1024 * 1024
            }, sp.GetRequiredService<ITaxCertificateStore>(), sp.GetRequiredService<TimeProvider>());
        });
        services.AddSingleton<ITaxPaymentTransport>(sp => (ITaxPaymentTransport)sp.GetRequiredService<ITaxAuthorityClient>());
        services.AddScoped<ICommandHandler<SubmitTaxPaymentCommand, TaxPaymentState>, SubmitTaxPaymentCommandHandler>();
        services.AddScoped<ICommandHandler<SubmitTaxInvoiceCommand, TaxDispatchState>, SubmitTaxInvoiceCommandHandler>();
        services.AddScoped<ICommandHandler<InquireTaxSubmissionCommand, TaxDispatchState>, InquireTaxSubmissionCommandHandler>();
        services.AddScoped<ICommandHandler<InquireTaxWorkFolderCommand, TaxWorkFolderState>, InquireTaxWorkFolderCommandHandler>();
        services.AddScoped<ITaxDispatchQueue, TaxDispatchQueue>();
        services.AddScoped<ICommandHandler<EnqueueTaxReferenceCommand, Guid>, EnqueueTaxReferenceCommandHandler>();
        services.AddScoped<ICommandHandler<EnqueueTaxInvoiceCommand, Guid>, EnqueueTaxInvoiceCommandHandler>();
        services.AddScoped<ITaxationRepository, TaxationRepository>();
        services.AddScoped<ITaxInvoiceListing, TaxInvoiceListing>();
        services.AddScoped<IQueryHandler<ListTaxInvoicesQuery, TaxInvoicePage>, ListTaxInvoicesQueryHandler>();
        services.AddScoped<ICommandHandler<SaveTaxMemoryCommand, Guid>, SaveTaxMemoryCommandHandler>();
        services.AddScoped<ICommandHandler<ActivateTaxMemoryCommand, bool>, ActivateTaxMemoryCommandHandler>();
        services.AddScoped<IQueryHandler<ListTaxMemoriesQuery, IReadOnlyList<TaxMemory>>, ListTaxMemoriesQueryHandler>();
        services.AddScoped<IQueryHandler<PreviewTaxInvoiceQuery, InvoicePreview>, PreviewTaxInvoiceQueryHandler>();
        services.AddScoped<ICommandHandler<SaveTaxPreviewCommand, Guid>, SaveTaxPreviewCommandHandler>();
        services.AddScoped<ICommandHandler<SetTaxProductMappingCommand, bool>, SetTaxProductMappingCommandHandler>();
    }
}

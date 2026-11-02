using System.Collections.Generic;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.EndPoint.FrontEnd.Services.Accountancies;
using CRM.Infrastructures.Sql.Accountancies.Repositories;
using Microsoft.Extensions.DependencyInjection;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyDetails;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyTransfer;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetInvoiceTotals;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyCartable;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyPayments;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetCustomerWallet;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountanciesTable;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetBankAccountOptions;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetBankAccountsTable;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetBankAccountDetails;
using CRM.Core.ApplicationServices.Accountancies.Commands.DelAccountancy;
using CRM.Core.ApplicationServices.Accountancies.Commands.AddInvoiceWalletToPf;
using CRM.Core.ApplicationServices.Accountancies.Commands.UpBankAccount;
using CRM.Core.ApplicationServices.Accountancies.Commands.AddBankAccount;
using CRM.Core.ApplicationServices.Accountancies.Commands.ChangeActiveBankAccount;
using CRM.Core.ApplicationServices.Accountancies.Commands.DelBankAccountItem;
using CRM.Core.ApplicationServices.Accountancies.Commands.AcceptAccountancy;
using CRM.Core.ApplicationServices.Accountancies.Commands.ChangeStatusAllAcc;
using CRM.Core.ApplicationServices.Accountancies.Commands.ChangeStatusAcc;
using CRM.Core.ApplicationServices.Accountancies.Commands.AddAccountancyTransfer;
using CRM.Core.ApplicationServices.Accountancies.Commands.RemoveAccountancyTransfer;
using CRM.Core.ApplicationServices.Accountancies.Commands.RemoveVosolInvoice;

namespace CRM.EndPoint.FrontEnd.Composition;
public static class AccountancyServicesRegistration
{
    public static IServiceCollection AddAccountancyServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountancyRepository, AccountancyRepository>();
        services.AddScoped<IAccountancyEffects, AccountancyEffects>();
        services.AddScoped<IQueryHandler<GetAccountancyDetailsQuery, AccountancyDetails>, GetAccountancyDetailsQueryHandler>();
        services.AddScoped<IQueryHandler<GetAccountancyTransferQuery, AccountancyTransfer?>, GetAccountancyTransferQueryHandler>();
        services.AddScoped<IQueryHandler<GetInvoiceTotalsQuery, PaymentTotalsData?>, GetInvoiceTotalsQueryHandler>();
        services.AddScoped<IQueryHandler<GetAccountancyCartableQuery, object>, GetAccountancyCartableQueryHandler>();
        services.AddScoped<IQueryHandler<GetAccountancyPaymentsQuery, object>, GetAccountancyPaymentsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCustomerWalletQuery, List<Accountancy>>, GetCustomerWalletQueryHandler>();
        services.AddScoped<IQueryHandler<GetAccountanciesTableQuery, AccountancyTable>, GetAccountanciesTableQueryHandler>();
        services.AddScoped<IQueryHandler<GetBankAccountOptionsQuery, object>, GetBankAccountOptionsQueryHandler>();
        services.AddScoped<IQueryHandler<GetBankAccountsTableQuery, BankAccountTable>, GetBankAccountsTableQueryHandler>();
        services.AddScoped<IQueryHandler<GetBankAccountDetailsQuery, BankAccount?>, GetBankAccountDetailsQueryHandler>();
        services.AddScoped<ICommandHandler<DelAccountancyCommand, AccountancyOutcome>, DelAccountancyCommandHandler>();
        services.AddScoped<ICommandHandler<AddInvoiceWalletToPfCommand, AccountancyOutcome>, AddInvoiceWalletToPfCommandHandler>();
        services.AddScoped<ICommandHandler<UpBankAccountCommand, AccountancyOutcome>, UpBankAccountCommandHandler>();
        services.AddScoped<ICommandHandler<AddBankAccountCommand, AccountancyOutcome>, AddBankAccountCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeActiveBankAccountCommand, AccountancyOutcome>, ChangeActiveBankAccountCommandHandler>();
        services.AddScoped<ICommandHandler<DelBankAccountItemCommand, AccountancyOutcome>, DelBankAccountItemCommandHandler>();
        services.AddScoped<ICommandHandler<AcceptAccountancyCommand, AccountancyOutcome>, AcceptAccountancyCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeStatusAllAccCommand, AccountancyOutcome>, ChangeStatusAllAccCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeStatusAccCommand, AccountancyOutcome>, ChangeStatusAccCommandHandler>();
        services.AddScoped<ICommandHandler<AddAccountancyTransferCommand, AccountancyOutcome>, AddAccountancyTransferCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveAccountancyTransferCommand, AccountancyOutcome>, RemoveAccountancyTransferCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveVosolInvoiceCommand, AccountancyOutcome>, RemoveVosolInvoiceCommandHandler>();
        return services;
    }
}

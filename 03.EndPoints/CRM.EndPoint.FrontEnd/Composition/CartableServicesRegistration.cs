using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Cartables.Abstractions;
using CRM.Core.Domain.Cartables.Repositories;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.EndPoint.FrontEnd.Services.Cartables;
using CRM.Infrastructures.Sql.Cartables.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartablePreFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSupply;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFinancial;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSales;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableOwnPreFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableModalFactors;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableEnvelope;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartablePreFactorsHtml;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFactorsHtml;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableModalFactorsHtml;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSupplyHtml;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableFinancialHtml;
using CRM.Core.ApplicationServices.Cartables.Queries.GetCartableSalesHtml;

namespace CRM.EndPoint.FrontEnd.Composition;
public static class CartableServicesRegistration
{
    public static IServiceCollection AddCartableServices(this IServiceCollection services)
    {
        services.AddScoped<ICartableRepository, CartableRepository>();
        services.AddScoped<ICartableEffects, CartableEffects>();
        services.AddScoped<ICartablePresentation, CartablePresentation>();
        services.AddScoped<IQueryHandler<GetCartablePreFactorsQuery, CartablePreFactors>, GetCartablePreFactorsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableFactorsQuery, CartableFactorData>, GetCartableFactorsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableSupplyQuery, CartableSupplyResult>, GetCartableSupplyQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableFinancialQuery, CartableFinancialData>, GetCartableFinancialQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableSalesQuery, CartableSalesData>, GetCartableSalesQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableOwnPreFactorsQuery, List<PreFactor>>, GetCartableOwnPreFactorsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableModalFactorsQuery, CartableFactorData>, GetCartableModalFactorsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableEnvelopeQuery, object>, GetCartableEnvelopeQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartablePreFactorsHtmlQuery, object>, GetCartablePreFactorsHtmlQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableFactorsHtmlQuery, object>, GetCartableFactorsHtmlQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableModalFactorsHtmlQuery, object>, GetCartableModalFactorsHtmlQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableSupplyHtmlQuery, object>, GetCartableSupplyHtmlQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableFinancialHtmlQuery, object>, GetCartableFinancialHtmlQueryHandler>();
        services.AddScoped<IQueryHandler<GetCartableSalesHtmlQuery, object>, GetCartableSalesHtmlQueryHandler>();
        return services;
    }
}

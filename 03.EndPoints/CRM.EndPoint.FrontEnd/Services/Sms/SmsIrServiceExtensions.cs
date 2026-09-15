using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CRM.EndPoint.FrontEnd.Services.Sms;

public static class SmsIrServiceExtensions {
    public static IServiceCollection AddSmsIr(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmsIrOptions>(configuration.GetSection("SmsIr"));

        services.AddHttpClient<ISmsIrService, SmsIrService>((serviceProvider, client) => {
            var options = serviceProvider.GetRequiredService<IOptions<SmsIrOptions>>()?.Value;

            client.BaseAddress = new Uri(options.ApiUrl.TrimEnd('/') + "/");

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Add("X-API-KEY", options.ApiKey);

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
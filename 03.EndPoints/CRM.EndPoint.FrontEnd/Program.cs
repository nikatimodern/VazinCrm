using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Enrichers.ShortTypeName;
using Serilog.Events;

namespace CRM.EndPoint.FrontEnd {
    public class Program {
        public static void Main(string[] args)
        {
            ConfigureLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application crashed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

        private static void ConfigureLogger()
        {
            Log.Logger = new LoggerConfiguration()

                //.MinimumLevel.Error()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error) // مایکروسافت فقط ارور
                .MinimumLevel.Override("System", LogEventLevel.Error) // سیستم فقط ارور
                .Enrich.FromLogContext()
                .Enrich.WithShortTypeName()
                .WriteTo.Async(x =>
                    x.File(
                        "Logs/log-.txt",
                        rollingInterval:
                        RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        shared: true,
                        encoding: Encoding.UTF8,
                        outputTemplate:
                        "[{Timestamp:HH:mm:ss}] " +
                        "[{Level:u3}] " +
                        "[{SourceContext}] " +
                        "{Message:lj}" +
                        "{NewLine}{Exception}"
                    )
                )
                .CreateLogger();
        }
    }
}
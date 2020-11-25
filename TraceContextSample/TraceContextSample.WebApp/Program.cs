using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;
using TraceContextSample.Logging;
using Microsoft.Extensions.DependencyInjection;
using TraceContextSample.Web.Enrichers;

namespace TraceContextSample.WebApp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Information()
            //    .Enrich.FromLogContext()
            //    .WriteTo.Console(formatter: new JsonFormatter())
            //    .CreateLogger();
            try
            {
                Log.Information("Starting host...");

                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly.");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog(
                    (context, services, configuration) =>
                    configuration.
                        MinimumLevel.Information()
                        .Enrich.FromLogContext()
                        .Enrich.With(services.GetService<ClaimsEnricher>())
                        .WriteTo.Console(formatter: new JsonFormatter())

                    )
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

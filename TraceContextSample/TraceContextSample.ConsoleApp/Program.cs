using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Threading.Tasks;
using Serilog.Formatting.Json;
using TraceContextSample.Net;
using TraceContextSample.Net.Clients;

namespace TraceContextSample.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console(new JsonFormatter())
                //.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}ConnectionId:{ConnectionId},SpanId:{SpanId},TraceId:{TraceId},RequestId:{RequestId},ParentId:{ParentId},{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            await CreateHostBuilder(args)
                .Build()
                .RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient<ITokenClient, TokenClient>();
                    services.AddHttpClient<IBffClient, BffClient>();

                    services.AddTransient<App>();
                    services.AddHostedService<AppService>();
                })
        ;
    }
}

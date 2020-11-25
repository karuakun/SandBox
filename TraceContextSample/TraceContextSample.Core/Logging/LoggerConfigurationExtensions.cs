using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace TraceContextSample.Logging
{
    public static class LoggerConfigurationExtensions
    {
        public static LoggerConfiguration CreateWebSiteDefaultLoggerConfiguration(this LoggerConfiguration loggerConfiguration)
        {
            return loggerConfiguration
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    //.WriteTo.Console(formatter: new JsonFormatter())
                    .WriteTo.Console(
                        outputTemplate:
                        "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}ConnectionId:{ConnectionId},SpanId:{SpanId},TraceId:{TraceId},RequestId:{RequestId},ParentId:{ParentId},{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                        theme: AnsiConsoleTheme.Code)
                ;
        }
        public static LoggerConfiguration CreateWebApiDefaultLoggerConfiguration(this LoggerConfiguration loggerConfiguration)
        {
            return loggerConfiguration
                    .MinimumLevel.Warning()
                    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker", LogEventLevel.Information)
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    //.WriteTo.Console(new JsonFormatter())
                    .WriteTo.Console(
                        outputTemplate:
                        "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}ConnectionId:{ConnectionId},SpanId:{SpanId},TraceId:{TraceId},RequestId:{RequestId},ParentId:{ParentId},{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                        theme: AnsiConsoleTheme.Code)
                ;
        }
    }
}

using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

namespace TraceContextSample.Logging
{
    public class LoggerConfigurationFactory
    {
        public static LoggerConfiguration CreateWebSiteDefaultLoggerConfiguration()
        {
            return new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    //.WriteTo.Console(formatter: new JsonFormatter())
                    .WriteTo.Console(
                        outputTemplate:
                        "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}ConnectionId:{ConnectionId},SpanId:{SpanId},TraceId:{TraceId},RequestId:{RequestId},ParentId:{ParentId},{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                        theme: AnsiConsoleTheme.Code)
                ;
        }
        public static LoggerConfiguration CreateWebApiDefaultLoggerConfiguration()
        {
            return new LoggerConfiguration()
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

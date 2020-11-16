using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TraceContextSample.ConsoleApp
{
    public class AppService : IHostedService
    {
        private readonly ILogger<AppService> _logger;
        private readonly App _app;

        public AppService(ILogger<AppService> logger, App app)
        {
            _logger = logger;
            _app = app;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StartAsync");
            await _app.RunAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StopAsync");
            return Task.CompletedTask;
        }
    }
}

using System.Linq;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace TraceContextSample.Web.Enrichers
{
    public class ClaimsEnricher: ILogEventEnricher
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClaimsEnricher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user != null && user.Identity.IsAuthenticated)
            {
                var ciamls = user.Identities.First().Claims;
                var userId = ciamls.FirstOrDefault(c => c.Type.ToLower() == "sub");
                if (userId != null)
                {
                    var userIdProperty = propertyFactory.CreateProperty("UserId", userId);
                    logEvent.AddPropertyIfAbsent(userIdProperty);
                }
            }
        }
    }
}

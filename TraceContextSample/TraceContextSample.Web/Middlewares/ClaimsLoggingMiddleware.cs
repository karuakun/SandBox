using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace TraceContextSample.Web.Middlewares
{
    public class ClaimsLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public ClaimsLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var user = httpContext.User;
            if (user.Identity.IsAuthenticated)
            {
                var ciamls = user.Identities.First().Claims;
                var userId = ciamls.FirstOrDefault(c => c.Type.ToLower() == "sub");
                if (userId != null)
                {
                    using (LogContext.PushProperty("UserId", userId?.Value))
                        await _next(httpContext);
                }
            }
        }
    }
}

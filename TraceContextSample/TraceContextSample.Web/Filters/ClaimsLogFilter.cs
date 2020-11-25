using Microsoft.AspNetCore.Mvc.Filters;
using Serilog.Context;
using System.Linq;

namespace TraceContextSample.Web.Filters
{
    public class ClaimsLogFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity.IsAuthenticated)
            {
                var ciamls = user.Identities.First().Claims;
                var userId = ciamls.FirstOrDefault(c => c.Type.ToLower() == "sub");
                if (userId != null)
                {
                    LogContext.PushProperty("UserId", userId);
                }
            }
        }
    }
}

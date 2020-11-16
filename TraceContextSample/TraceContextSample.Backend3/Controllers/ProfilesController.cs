using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TraceContextSample.Backend3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfilesController : ControllerBase
    {
        [Route("{id}")]
        public async Task<string> GetProfile([FromRoute] string id)
        {
            await Task.CompletedTask;
            return "abc";
        }
    }
}

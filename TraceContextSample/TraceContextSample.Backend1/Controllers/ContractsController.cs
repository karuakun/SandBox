using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceContextSample.Net.Interfaces;

namespace TraceContextSample.Backend1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContractsController : ControllerBase
    {
        [Route("{id}")]
        public Contract GetContract([FromRoute] string id)
        {
            return new Contract
            {
                Id = Guid.NewGuid().ToString(),
                Name = "xxxxxxx"
            };
        }
    }


}

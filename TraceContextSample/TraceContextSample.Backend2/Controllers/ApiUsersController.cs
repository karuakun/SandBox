using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceContextSample.Net.Clients;
using TraceContextSample.Net.Interfaces;

namespace TraceContextSample.Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ApiUsersController : Controller
    {
        private readonly ITokenClient _tokenClient;
        private readonly IBackend3Client _backend3Client;

        public ApiUsersController(ITokenClient tokenClient, IBackend3Client backend3Client)
        {
            _tokenClient = tokenClient;
            _backend3Client = backend3Client;
        }

        [Route("{id}")]
        public async Task<ApiUser> GetContract([FromRoute] string id)
        {
            var token = await _tokenClient.RequestTokenAsync(new TokenSettings
                {
                    AuthorityBaseUri = Constants.Authority.BaseUri
                },
                Constants.Backend2.ClientId,
                Constants.Backend2.ClientSecret,
                new []{ Constants.Backend3.ResourceName});
            await _backend3Client.GetProfileAsync("1", token.IdentityToken);
            return new ApiUser
            {
                Id = Guid.NewGuid().ToString(),
                Name = "yyyyyy",
                Age = 10
            };
        }
    }
}

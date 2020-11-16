using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceContextSample.Net.Clients;
using TraceContextSample.Net.Interfaces;

namespace TraceContextSample.Bff.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ServiceUsersController : ControllerBase
    {
        private readonly ITokenClient _tokenClient;
        private readonly IBackend1Client _backend1Client;
        private readonly IBackend2Client _backend2Client;
        public ServiceUsersController(ITokenClient tokenClient, IBackend1Client backend1Client, IBackend2Client backend2Client)
        {
            _tokenClient = tokenClient;
            _backend1Client = backend1Client;
            _backend2Client = backend2Client;
        }
        public async Task<ServiceUser> GetServiceUsers()
        {
            var token = await _tokenClient.RequestTokenAsync(new TokenSettings
                {
                    AuthorityBaseUri = Constants.Authority.BaseUri
                },
                Constants.Bff.ClientId,
                Constants.Bff.ClientSecret,
                new[]
                {
                    Constants.Backend1.ResourceName,
                    Constants.Backend2.ResourceName,
                });

            var contract = await _backend1Client.GetContractAsync("1", token.AccessToken);
            var apiUser = await _backend2Client.GetApiUserAsync("1", token.AccessToken);

            return new ServiceUser
            {
                Jwt = (from c in User.Claims select new KeyValuePair<string, string>(c.Type, c.Value)).ToList(),
                Contract = contract,
                ApiUser = apiUser,
            };
        }
    }
}

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace TraceContextSample.Net.Clients
{
    public interface ITokenClient
    {
        Task<TokenResponse> RequestTokenAsync(TokenSettings settings, string clientId, string clientSecret, string[] requestScope);
    }

    public class TokenSettings
    {
        public string AuthorityBaseUri { get; set; }
    }

    public class TokenClient: ITokenClient
    {
        private readonly HttpClient _httpClient;

        public TokenClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TokenResponse> RequestTokenAsync(TokenSettings settings, string clientId, string clientSecret, string[] requestScope)
        {
            var disco = await _httpClient.GetDiscoveryDocumentAsync(settings.AuthorityBaseUri);
            if (disco.IsError) throw new Exception(disco.Error);

            var response = await _httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = string.Join(" ", requestScope)
            });
            if (response.IsError) throw new Exception(response.Error);
            return response;
        }
    }
}

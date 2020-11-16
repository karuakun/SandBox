using System.Net.Http;
using System.Threading.Tasks;
using TraceContextSample.Net.Interfaces;

namespace TraceContextSample.Net.Clients
{
    public interface IBffClient
    {
        Task<ServiceUser> GetServiceUsersAsync(string token);
    }
    public class BffClient: IBffClient
    {
        private readonly HttpClient _httpClient;
        public BffClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<ServiceUser> GetServiceUsersAsync(string token)
        {
            _httpClient.SetAccessToken(token);
            return await _httpClient.GetAsync<ServiceUser>(Constants.Bff.BaseUri + "/api/ServiceUsers");
        }
    }
}

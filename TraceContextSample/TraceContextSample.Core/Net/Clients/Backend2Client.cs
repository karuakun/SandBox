using System.Net.Http;
using System.Threading.Tasks;
using TraceContextSample.Net.Interfaces;

namespace TraceContextSample.Net.Clients
{
    public interface IBackend2Client
    {
        Task<ApiUser> GetApiUserAsync(string id, string token);
    }
    public class Backend2Client: IBackend2Client
    {
        private readonly HttpClient _httpClient;
        public Backend2Client(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiUser> GetApiUserAsync(string id, string token)
        {
            _httpClient.SetAccessToken(token);
            return await _httpClient.GetAsync<ApiUser>($"/api/ApiUsers/{id}");
        }
    }
}

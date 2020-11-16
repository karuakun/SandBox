using System.Net.Http;
using System.Threading.Tasks;

namespace TraceContextSample.Net.Clients
{
    public interface IBackend3Client
    {
        Task GetProfileAsync(string id, string token);
    }
    public class Backend3Client: IBackend3Client
    {
        private readonly HttpClient _httpClient;
        public Backend3Client(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task GetProfileAsync(string id, string token)
        {
            _httpClient.SetAccessToken(token);
            await _httpClient.GetAsync($"/api/Profiles/{id}");

        }
    }
}

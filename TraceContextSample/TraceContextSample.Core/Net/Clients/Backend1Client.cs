using System;
using System.Net.Http;
using System.Threading.Tasks;
using TraceContextSample.Net.Interfaces;

namespace TraceContextSample.Net.Clients
{
    public interface IBackend1Client
    {
        Task<Contract> GetContractAsync(string id, string token);
    }

    public class Backend1Client : IBackend1Client
    {
        private readonly HttpClient _httpClient;
        public Backend1Client(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Contract> GetContractAsync(string id, string token)
        {
            _httpClient.SetAccessToken(token);
            return await _httpClient.GetAsync<Contract>($"/api/Contracts/{id}");
        }
    }
}

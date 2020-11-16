using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TraceContextSample.Net
{
    public static class HttpClientExtensions
    {
        public static void SetAccessToken(this HttpClient source, string accessToken)
        {
            source.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public static async Task<TEntity> GetAsync<TEntity>(this HttpClient source, string url)
        {
            using (var response = await source.GetAsync(url))
            {
                if (!response.IsSuccessStatusCode) return default;
                using (var content = response.Content)
                {
                    var d = await content.ReadAsStringAsync();
                    if (d != null)
                    {
                        return JsonConvert.DeserializeObject<TEntity>(d);
                    }
                }
            }
            return default;
        }
    }
}

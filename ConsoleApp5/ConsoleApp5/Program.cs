using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var target = Environment.GetEnvironmentVariable("TARGET");
            var include = Environment.GetEnvironmentVariable("INCLUDE");
            var exclude = Environment.GetEnvironmentVariable("EXCLUDE");
            var app = new App(target);
            await app.LinkCheck(target, "root",
                string.IsNullOrEmpty(include)
                    ? new string[] { }
                    : include.Split(","),
                string.IsNullOrEmpty(exclude)
                    ? new string[] { }
                    : exclude.Split(","));
        }
    }

    public class App
    {
        private readonly HttpClient _httpClient;
        public Dictionary<string, RequestStatus> RequestStatus { get; } = new Dictionary<string, RequestStatus>();

        public App(string targetUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(targetUrl)
            };
        }
        
        public async Task<RequestStatus> LinkCheck(string requestUrl, string refUrl, string[] includeUrls, string[] excludeUrls)
        {
            if (RequestStatus.ContainsKey(requestUrl))
            {
                RequestStatus[requestUrl].RefUrls.Add(refUrl);
                return RequestStatus[requestUrl];
            }
            RequestStatus.TryAdd(requestUrl, new RequestStatus
            {
                Url = requestUrl,
                RefUrls = new List<string> { refUrl }
            });

            var status = RequestStatus[requestUrl];
            if (!includeUrls.Any(requestUrl.Contains))
                return status;
            if (excludeUrls.Any(requestUrl.Contains))
                return status;

            var response = await _httpClient.GetAsync(requestUrl);
            status.RequestTime = DateTimeOffset.Now;
            status.Status = response.StatusCode;
            status.RefUrls.Add(refUrl);

            WriteLog($"{status.RequestTime},{response.StatusCode},{requestUrl},{refUrl}");
            await Task.Delay(500);

            if (!response.IsSuccessStatusCode) 
                return status;

            var linkUrls = ExtractUrls(await response.Content.ReadAsStringAsync());
            foreach (var childUrl in linkUrls)
                await LinkCheck(childUrl, requestUrl, includeUrls, excludeUrls);
            return status;
        }

        private void WriteLog(string log)
        {
            Console.WriteLine(log);
        }

        public string[] ExtractUrls(string html)
        {
            var urls = Regex.Matches(html, @"https?://[\w!?/+\-_~=;.,*&@#$%()'[\]]+");
            return urls.Any()
                ? urls.Select(_ => _.Value.ToLower()).Distinct().ToArray()
                : new string[] { };
        }
    }
    public class RequestStatus
    {
        public string Url { get; set; }
        public List<string> RefUrls { get; set; }
        public HttpStatusCode Status { get; set; }
        public DateTimeOffset RequestTime { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    public class Worker
    {
        private readonly HttpClient _httpClient;
        private readonly AppHtmlParser _appHtmlParser = new AppHtmlParser();
        public Dictionary<string, RequestStatus> RequestStatus { get; } = new Dictionary<string, RequestStatus>();

        private readonly string[] _includeRequestStrings;
        private readonly string[] _excludeRequestStrings;
        private readonly string[] _includeReferencedStrings;
        private readonly string[] _excludeReferencedStrings;

        public Worker(string targetUrl, 
            string[] includeRequestStrings, 
            string[] excludeRequestStrings,
            string[] includeReferencedStrings,
            string[] excludeReferencedStrings)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(targetUrl)
            };
            _includeRequestStrings = includeRequestStrings;
            _excludeRequestStrings = excludeRequestStrings;
            _includeReferencedStrings = includeReferencedStrings;
            _excludeReferencedStrings = excludeReferencedStrings;
        }

        public async Task<RequestStatus> Run(string requestUrl)
        {
            return await LinkCheck(requestUrl, "root");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestUrl">リクエストURL</param>
        /// <param name="referencedPageUrl">参照元URL</param>
        /// <returns></returns>
        private async Task<RequestStatus> LinkCheck(string requestUrl, string referencedPageUrl)
        {
            if (RequestStatus.ContainsKey(requestUrl))
            {
                RequestStatus[requestUrl].ReferencedPageUrls.Add(referencedPageUrl);
                return RequestStatus[requestUrl];
            }
            RequestStatus.TryAdd(requestUrl, new RequestStatus
            {
                Url = requestUrl,
                ReferencedPageUrls = new List<string> { referencedPageUrl }
            });

            var status = RequestStatus[requestUrl];
            if (_includeRequestStrings.Any() && !_includeRequestStrings.Any(requestUrl.Contains))
                return status;
            if (_excludeRequestStrings.Any() && _excludeRequestStrings.Any(requestUrl.Contains))
                return status;
            if (_includeReferencedStrings.Any() && !_includeReferencedStrings.Any(referencedPageUrl.Contains))
                return status;
            if (_excludeReferencedStrings.Any() && _excludeReferencedStrings.Any(referencedPageUrl.Contains))
                return status;


            var response = await GetRequest(requestUrl);
            if (response == null)
            {
                WriteLog($"{status.RequestTime}, Error, {requestUrl}, {referencedPageUrl}");
                return status;
            }

            status.RequestTime = DateTimeOffset.Now;
            status.Status = response.StatusCode;
            status.ReferencedPageUrls.Add(referencedPageUrl);

            WriteLog($"{status.RequestTime}, {response.StatusCode}, {requestUrl}, {referencedPageUrl}");
            await Task.Delay(200);

            if (!response.IsSuccessStatusCode) 
                return status;

            var linkUrls = await _appHtmlParser.ExtractUrls(response.RequestMessage.RequestUri, await response.Content.ReadAsStringAsync());
            foreach (var childUrl in linkUrls)
                await LinkCheck(childUrl, requestUrl);

            return status;
        }

        private async Task<HttpResponseMessage> GetRequest(string requestUrl)
        {
            try
            {
                return await _httpClient.GetAsync(requestUrl);
            }
            catch (Exception e)
            {
                return null;
            }
        }
        private void WriteLog(string log)
        {
            Console.WriteLine(log);
        }
    }
}
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TraceContextSample.Net.Clients;

namespace TraceContextSample.ConsoleApp
{
    public class App
    {
        private readonly ITokenClient _tokenClient;
        private readonly IBffClient _bffClient;

        public App(ITokenClient tokenClient, IBffClient bffClient)
        {
            _tokenClient = tokenClient;
            _bffClient = bffClient;
        }

        public async Task RunAsync()
        {
            while (true)
            {
                using var activity = new Activity(nameof(RunAsync)).Start();
                try
                {
                    Console.WriteLine("トークンを取得し、APIを呼びだします。");
                    Console.ReadKey();

                    var response = await _tokenClient.RequestTokenAsync(
                        new TokenSettings {AuthorityBaseUri = Constants.Authority.BaseUri},
                        Constants.ConsoleApp.ClientId,
                        Constants.ConsoleApp.ClientSecret,
                        new[] {Constants.Bff.ResourceName});
                    response.DumpConsole();

                    var data = await _bffClient.GetServiceUsersAsync(response.AccessToken);
                    Console.WriteLine(JsonConvert.SerializeObject(data));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    activity.Stop();
                }
            }
        }
    }
}

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TraceContextSample.Net.Clients;
using TraceContextSample.WebApp.Models;

namespace TraceContextSample.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITokenClient _tokenClient;
        private readonly IBffClient _bffClient;

        public HomeController(ILogger<HomeController> logger, ITokenClient tokenClient, IBffClient bffClient)
        {
            _logger = logger;
            _tokenClient = tokenClient;
            _bffClient = bffClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        
        [Authorize]
        public async Task<IActionResult> Api()
        {
            var token = await _tokenClient.RequestTokenAsync(new TokenSettings {AuthorityBaseUri = Constants.Authority.BaseUri},
                Constants.WebApp.ClientId,
                Constants.WebApp.ClientSecret,
                new[]
                {
                    Constants.Bff.ResourceName
                });
            var result = await _bffClient.GetServiceUsersAsync(token.AccessToken);
            var json = JsonConvert.SerializeObject(result.ApiUser);
            ViewBag.ApiUser = json;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

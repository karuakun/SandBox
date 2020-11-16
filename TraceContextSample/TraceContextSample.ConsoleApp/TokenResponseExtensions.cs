using System;
using System.Text;
using IdentityModel;
using IdentityModel.Client;
using Newtonsoft.Json.Linq;

namespace TraceContextSample.ConsoleApp
{
    public static class TokenResponseExtensions
    {
        public static void DumpConsole(this TokenResponse response)
        {
            if (response.AccessToken.Contains("."))
            {

                var parts = response.AccessToken.Split('.');
                var header = parts[0];
                var claims = parts[1];

                Console.WriteLine(JObject.Parse(Encoding.UTF8.GetString(Base64Url.Decode(header))));
                Console.WriteLine(JObject.Parse(Encoding.UTF8.GetString(Base64Url.Decode(claims))));
            }
        }
    }
}

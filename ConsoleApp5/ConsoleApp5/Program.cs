using System;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var target = Environment.GetEnvironmentVariable("TARGET");
            var includeRequestStrings = Environment.GetEnvironmentVariable("INCLUDE_REQUEST_STRINGS");
            var excludeRequestStrings = Environment.GetEnvironmentVariable("EXCLUDE_REQUEST_STRINGS");
            var includeReferencedStrings = Environment.GetEnvironmentVariable("INCLUDE_REFERENCED_STRINGS");
            var excludeReferencedStrings = Environment.GetEnvironmentVariable("EXCLUDE_REFERENCED_STRINGS");

            static string[] ParseArrayParameter(string parameter) =>
                string.IsNullOrEmpty(parameter)
                    ? new string[] { }
                    : parameter.Split(",");

            var app = new Worker(target,
                ParseArrayParameter(includeRequestStrings),
                ParseArrayParameter(excludeRequestStrings),
                ParseArrayParameter(includeReferencedStrings),
                ParseArrayParameter(excludeReferencedStrings)
            );

            await app.Run(target);
        }
    }
}

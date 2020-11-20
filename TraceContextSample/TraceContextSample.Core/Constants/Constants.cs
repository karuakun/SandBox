using System.Collections.Generic;

namespace TraceContextSample.Constants
{
    public class Authority
    {
        public const string BaseUri = "https://localhost:5011";
    }
    public class ConsoleApp
    {
        public const string ClientId = "console_client";
        public const string ClientSecret = "console_client_secret";
    }
    public class Bff
    {
        public const string BaseUri = "https://localhost:5021";
        public const string ClientId = "bff";
        public const string ClientSecret = "bff_secret";
        public const string ResourceName = "bff";
    }
    public class Backend1
    {
        public const string BaseUri = "https://localhost:5031";
        public const string ResourceName = "backend1";
    }
    public class Backend2
    {
        public const string BaseUri = "https://localhost:5041";
        public const string ClientId = "backend2";
        public const string ClientSecret = "backend2_secret";
        public const string ResourceName = "backend2";
    }
    public class Backend3
    {
        public const string BaseUri = "https://localhost:5051";
        public const string ResourceName = "backend3";
    }
    public class WebApp
    {
        public const string BaseUri = "https://localhost:5061";
        public const string ClientId = "web_client";
        public const string ClientSecret = "web_client_secret";

        public static readonly IEnumerable<string> Scopes = new[]
        {
            "openid", "profile", "offline_access", "email", Bff.ResourceName
        };
        public static readonly IEnumerable<string> RedirectUris = new[] {
            "https://localhost:5061/signin-oidc",
            "http://localhost:5060/signin-oidc",
        };
    }
    public class WebApp2
    {
        public const string BaseUri = "https://localhost:5071";
        public const string ClientId = "web_client2";
        public const string ClientSecret = "web_client2_secret";

        public static readonly IEnumerable<string> Scopes = new[]
        {
            "openid", "profile", "offline_access", "email"
        };
        public static readonly IEnumerable<string> RedirectUris = new[] {
            "https://localhost:5071/signin-oidc",
            "http://localhost:5070/signin-oidc",
        };
    }
}

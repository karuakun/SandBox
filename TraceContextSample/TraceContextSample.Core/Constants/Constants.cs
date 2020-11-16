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
}

using System.Collections.Generic;

namespace TraceContextSample.Net.Interfaces
{
    public class ServiceUser
    {
        public List<KeyValuePair<string, string>> Jwt { get; set; }
        public Contract Contract { get; set; }
        public ApiUser ApiUser { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Net;

namespace ConsoleApp5
{
    public class RequestStatus
    {
        public string Url { get; set; }
        public List<string> ReferencedPageUrls { get; set; }
        public HttpStatusCode Status { get; set; }
        public DateTimeOffset RequestTime { get; set; }
    }
}
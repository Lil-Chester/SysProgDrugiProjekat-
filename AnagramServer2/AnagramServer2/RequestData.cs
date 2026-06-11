using System.Net;

namespace AnagramServer2
{
    public class RequestData
    {
        public required HttpListenerContext Context { get; set; }

        public required string FileName { get; set; }

        public required string Word { get; set; }
    }
}
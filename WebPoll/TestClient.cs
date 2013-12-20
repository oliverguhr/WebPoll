using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebPoll
{    public class TestClient
    {
        private WebClient Client { get; set; }

        private Uri Uri { get; set; }

        public TestClient(Uri uri)
        {
            Client = new WebClient();
            Client.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            Client.Headers.Add(HttpRequestHeader.UserAgent, "LoadTest/" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Uri = uri;
        }

        public void AcceptCompression(bool accept)
        {
            if (accept)
            {
                Client.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate,sdch");
            }
            else
            {
                Client.Headers.Remove(HttpRequestHeader.AcceptEncoding);
            }
        }

        public TestResult Test()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var response = Client.DownloadString(Uri);
            stopWatch.Stop();
            return new TestResult
            {
                ContentEncoding = Client.ResponseHeaders.AllKeys.Contains("Content-Encoding") ? Client.ResponseHeaders.GetValues("Content-Encoding").First() : string.Empty,
                ContentLength = Client.ResponseHeaders.AllKeys.Contains("Content-Length") ? Client.ResponseHeaders.GetValues("Content-Length").First() : string.Empty,
                ElapsedMilliseconds = stopWatch.ElapsedMilliseconds
            };
        }
    }
}

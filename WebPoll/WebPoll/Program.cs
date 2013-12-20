using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebPoll
{
    class Program
    {
        private static object OutputLock = new Object();
        static void Main(string[] args)
        {

            List<TestResult> ResultSetCompressionEnabled = new List<TestResult>();
            List<TestResult> ResultSetCompressionDisabled = new List<TestResult>();
            Stopwatch testsPerSecond = new Stopwatch();
            

            int counter = 1;
            // TestClient testClient = new TestClient(new Uri("http://demobp.azurewebsites.net/"));

            Action<List<TestResult>, string> writeAverageStatistic = (testResults, label) =>
            {
                if (testResults.Count != 0)
                {
                    double average;
                    lock (testResults)
                    {
                        average = testResults.Average(x => x.ElapsedMilliseconds);
                    }                    
                    Console.WriteLine("{0:0.00}\t Average ElapsedMilliseconds {1}", average, label);
                }

            };

            Action<long> writeStatistic = (elapsedMilliseconds) =>
            {
                lock (OutputLock)
                {
                    if (!testsPerSecond.IsRunning)
                        testsPerSecond.Start();

                    Console.CursorTop = 0;
                    Console.WriteLine("{0} ms \t {1:0.} Test/s \t Total Test {2} ", elapsedMilliseconds, counter / testsPerSecond.Elapsed.TotalSeconds, counter++);
                    Console.CursorTop = 1;
                    writeAverageStatistic(ResultSetCompressionEnabled, "CompressionEnabled");
                    Console.CursorTop = 2;
                    writeAverageStatistic(ResultSetCompressionDisabled, "CompressionDisabled");
                }

            };


            Action<int> test = (i) =>
            {
                TestClient2 testClient = new TestClient2(new Uri("http://bdwordpress.azurewebsites.net/"));
                if (i % 2 == 0)
                {
                    testClient.AcceptCompression(true);
                }
                else
                {
                    testClient.AcceptCompression(false);
                }

                var result = testClient.Test();

                if (string.IsNullOrWhiteSpace(result.ContentEncoding))
                {
                    lock (ResultSetCompressionDisabled)
                    {
                        ResultSetCompressionDisabled.Add(result);
                    }

                }
                else
                {
                    lock (ResultSetCompressionEnabled)
                    {
                        ResultSetCompressionEnabled.Add(result);
                    }

                }
                writeStatistic(result.ElapsedMilliseconds);

            };
                  
           var demo =  Parallel.For(0, 10000, test);            

         //   for (int i = 1; i <= 1000; i++) test(i);

            //// The number of concurrent requests
            //int threads = 25;

            //// The number of total requests to execute
            //int requests = 100000;

            //// The remaining number of requests to execute
            //int requestsRemaining = requests;

            ////var sw = Stopwatch.StartNew();
            ////string query = "?ticks=" + Environment.TickCount.ToString();
            //for (int i = 0; i < threads; i++)
            //{
            //    var thread = new Thread(() =>
            //    {
            //        while (Interlocked.Decrement(ref requestsRemaining) > 0)
            //        {
            //            test(requestsRemaining);
            //        }

            //    });
            //    thread.Start();
            //}


            Console.ReadLine();
        }

    }

    public class TestClient2
    {
        private WebClient Client { get; set; }

        private Uri Uri { get; set; }

        public TestClient2(Uri uri)
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

    public class TestClient
    {
        private HttpWebRequest Request { get; set; }

        private Uri Uri { get; set; }

        public TestClient(Uri uri)
        {
            Uri = uri;
            Request = HttpWebRequest.CreateHttp(Uri);
            Request.UserAgent = "LoadTest/" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            
        }

        public void AcceptCompression(bool accept)
        {
            if (accept)
            {
                Request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate,sdch");
            }
            else
            {
                Request.Headers.Remove(HttpRequestHeader.AcceptEncoding);
            }          
        }

        public TestResult Test()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();          
            var response = (HttpWebResponse)Request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
          
            stopWatch.Stop();
            response.Close();
            return new TestResult
            {
                ContentEncoding = response.ContentEncoding,
                ContentLength = response.ContentLength.ToString(),
                ElapsedMilliseconds = stopWatch.ElapsedMilliseconds
            };
        }
    }

    public struct TestResult
    {
        public string ContentEncoding { get; set; }
        public string ContentLength { get; set; }
        public long ElapsedMilliseconds { get; set; }
    }
}

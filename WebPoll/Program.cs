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
            //insert load testing target here -> 
            Uri traget = new Uri("http://123.yourdomainhere.com/");

            List<TestResult> ResultSetCompressionEnabled = new List<TestResult>();
            List<TestResult> ResultSetCompressionDisabled = new List<TestResult>();
            Stopwatch testsPerSecond = new Stopwatch();            

            int counter = 1;            

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
                TestClient testClient = new TestClient(traget);
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
      
            Console.ReadLine();
        }

    }
}

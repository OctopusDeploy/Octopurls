using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Octopurls.Tests
{
    public class RedirectsURLsTests : OctopurlTest
    {
        [Fact]
        public void TestRedirectsURLs()
        {
            var query = from url in redirects.Urls.AsParallel().AsOrdered().WithDegreeOfParallelism(10)
                where TestUrl(url.Value) == false
                select url.Value;

            var badURLs = query.ToList();

            Assert.True(badURLs.Count == 0, $"The bad urls are:{Environment.NewLine}{string.Join(Environment.NewLine, badURLs)}");
        }

        [Fact]
        public void TestURLMethodFailsWithBadURLs()
        {
            var url = "https://totallyABadURL.com/Bad/Bad/Really/Really/Bad";
            Assert.False(TestUrl(url), $"Web request for the fake URL {url} did not fail, but it should have");
        }

        private bool TestUrl(string url)
        {
            //first try a HEAD which is usually faster 
            if (TestUrl(url, WebRequestMethods.Http.Head))
            {
                return true;
            }

            //Since not all pages support HEAD, then try doing a GET instead which is slower usually
            if (TestUrl(url, WebRequestMethods.Http.Get))
            {
                return true;
            }

            //if all the above fails, then its a bad URL
            return false;
        }

        private bool TestUrl(string url, string method)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 15000;
            request.AllowAutoRedirect = true;
            request.UseDefaultCredentials = true;

            request.Method = method;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return ValidateResponse(response,url);
                }
            }
            catch (WebException e)
            {
                var response = (HttpWebResponse)e.Response;

                if (response == null)
                {
                    Console.WriteLine($"Failure - [{method}] call to URL [{url}] returned message [{Environment.NewLine}{e.Message}]");
                    return false;
                }

                return ValidateResponse(response,url);
            }
        }

        private readonly List<int> acceptedStatusCodesOver400 = new List<int>()
        {
            403 //Some sites return 403 like carreers.stackOverflow if the job post has already been closed. The site still redirects user to a valid page.
        };

        private bool ValidateResponse(HttpWebResponse response,string url)
        {
            var statusCode = (int) response.StatusCode;

            if (statusCode >= 400 && !acceptedStatusCodesOver400.Contains(statusCode))
            {
                Console.WriteLine($"Failure - [{response.Method}] call to URL [{url}] returned status code [{statusCode}]");
                return false;
            }

            Console.WriteLine($"Success - [{response.Method}] call to URL [{url}] returned status code [{statusCode}] which is OK");
            return true;
        }
    }
}

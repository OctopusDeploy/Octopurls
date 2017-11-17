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
                    return ValidateResponse(response);
                }
            }
            catch (WebException e)
            {
                var statusCode = (e.Response as HttpWebResponse)?.StatusCode.ToString();

                Console.WriteLine($"Status code is: {statusCode}");

                if (acceptedStatusCodesOver400.Contains(statusCode))
                {
                    return true;
                }
                return false;
            }
        }

        private readonly List<string> acceptedStatusCodesOver400 = new List<string>()
        {
            "403" //Some sites return 403 like carreers.stackOverflow if the job post has already been closed. The site still redirects user to a valid page.
        };

        private bool ValidateResponse(HttpWebResponse response)
        {
            if ((int)response.StatusCode >= 400)
            {
                Console.WriteLine($"Failure - URL [{response.ResponseUri}] returned status code [{(int)response.StatusCode}] which means its a bad link");
                return false;
            }

            Console.WriteLine($"Success - URL [{response.ResponseUri}] returned status code [{(int)response.StatusCode}] which is cool");
            return true;
        }
    }
}

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
                where TestURL(url.Value) == false
                select url.Value;

            var badURLs = query.ToList();

            Assert.True(badURLs.Count == 0, $"The bad urls are:{Environment.NewLine}{string.Join(Environment.NewLine, badURLs)}");
        }

        [Fact]
        public void TestURLMethodFailsWithBadURLs()
        {
            var url = "https://totallyABadURL.com/Bad/Bad/Really/Really/Bad";
            Assert.False(TestURL(url), $"Web request for the fake URL {url} did not fail, but it should have");
        }

        private bool TestURL(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 15000;
            request.AllowAutoRedirect = true;
            request.UseDefaultCredentials = true;

            request.Method = "HEAD";

            try
            {
                //First trying with a HEAD request
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return ValidateResponse(response);
                }
            }
            catch (WebException e)
            {
                try
                {
                    HttpWebRequest secondrequest = (HttpWebRequest)WebRequest.Create(url);
                    secondrequest.Timeout = 15000;
                    secondrequest.AllowAutoRedirect = true;
                    secondrequest.UseDefaultCredentials = true;

                    secondrequest.Method = "GET";

                    //If the request for HEAD fails, try GET which is a bit more expensive. Lots of Microsoft links for some reason blow up doing a HEAD, but succeed with GET
                    using (HttpWebResponse response = (HttpWebResponse)secondrequest.GetResponse())
                    {
                        return ValidateResponse(response);
                    }

                }

                //If both HEAD and GET fail, then its definitely a bad URL 
                catch (WebException exception)
                {
                    //In this case the WebException doesn't return the status code, so we need to read it from the exception.Message
                    foreach (var code in _acceptedStatusCodesOver400)
                    {
                        if (exception.Message.Contains(code))
                        {
                            Console.WriteLine($"Success - Url [{url}] returned status code [{code}] which is in the list of accepted codes over 400");
                            return true;
                        }
                    }

                    Console.WriteLine($"Failure - URL [{url}] returned error [{exception.Message}]");
                    return false;
                }
            }
        }

        private readonly List<string> _acceptedStatusCodesOver400 = new List<string>()
        {
            "403" //Some sites return 403 like carreers.stackOverflow if the job post has already been closed. The site still redirects user to a valid page.
        };

        private bool ValidateResponse(HttpWebResponse response)
        {
            if ((int)response.StatusCode >= 400)
            {
                Console.WriteLine($"Failure - Url [{response.ResponseUri}] returned status code [{(int)response.StatusCode}] which means its a bad link");
                return false;
            }

            Console.WriteLine($"Success - Url [{response.ResponseUri}] returned status code [{(int)response.StatusCode}] which is cool");
            return true;
        }
    }
}

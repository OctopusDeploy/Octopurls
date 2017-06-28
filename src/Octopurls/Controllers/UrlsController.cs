using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using Mindscape.Raygun4Net;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Octopurls
{
    [Route("")]
    public class UrlsController : Controller
    {
        readonly Redirects redirects;
        readonly RaygunSettings raygun;
        readonly IConfiguration configuration;

        public UrlsController(Redirects redirects, RaygunSettings raygun, IConfiguration configuration)
        {
            this.redirects = redirects;
            this.raygun = raygun;
            this.configuration = configuration;
        }

        [HttpGet("")]
        public IActionResult Get()
        {
            return new RedirectResult("https://octopus.com");
        }

        [HttpGet("ping")]
        public ContentResult GetPong() // This is so we can pingdom monitor just this app, and not follow redirects
        {
            return Content("pong", "text/plain", Encoding.UTF8);
        }

        [HttpGet("{url}")]
        public async Task<IActionResult> Get(string url)
        {
            Console.WriteLine("Finding redirect for shortened URL '{0}' among {1} redirects", url, redirects.Urls.Count);
            try
            {
                string tmpRedirectUrl;
              if (redirects.Urls.TryGetValue(url, out tmpRedirectUrl))
                {
                    string redirectUrl;
                    if (Request.Query.Count <= 0)
                    {
                         redirectUrl = tmpRedirectUrl;
                    }
                    else
                    {
                        // Append Query String if supplied
                        var uriBuilder = new UriBuilder(tmpRedirectUrl);
                        uriBuilder.Query = string.Join("&", Request.Query.Select(x => $"{x.Key}={x.Value}").ToArray());
                        redirectUrl = uriBuilder.ToString();
                    }
                    Console.WriteLine("Found shortened URL '{0}' which redirects to '{1}'", url, redirectUrl);
                    return new RedirectResult(redirectUrl);
                }
                throw new KeyNotFoundException("Could not find shortened URL '" + url + "' in the list of configured redirects.");
            }
            catch (KeyNotFoundException kne)
            {
                Console.WriteLine(kne);
                try
                {
                    if (!String.IsNullOrWhiteSpace(raygun.ApiKey))
                    {
                        var message = RaygunMessageBuilder.New
                            .SetVersion("1.0.0")
                            .SetUserCustomData(new Dictionary<string, string> { {"Url", url} })
                            .SetExceptionDetails(kne)
                            .Build();
                        
                        var content = JsonConvert.SerializeObject(message);
    
                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders
                            .Accept
                            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        httpClient.DefaultRequestHeaders.Add("X-ApiKey", raygun.ApiKey);
    
                        var result = await httpClient.PostAsync("https://api.raygun.io/entries", new StringContent(content));
                        result.EnsureSuccessStatusCode();
                    }
                    var fuzzy = Fuzzy.Search(url, redirects.Urls.Keys.ToList());
                    var suggestions = redirects.Urls.Where(u=>fuzzy.Contains(u.Key)).ToDictionary(s=>s.Key, s=>s.Value);
                    ViewBag.Url = url;
                    return View("404", suggestions);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    return HttpBadRequest(ex);
                }
            }
        }
    }
}

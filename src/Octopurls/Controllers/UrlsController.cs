using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Octopurls
{
    [Route("")]
    public class UrlsController : Controller
    {
        readonly Redirects redirects;
        readonly IConfiguration configuration;

        public UrlsController(Redirects redirects, IConfiguration configuration)
        {
            this.redirects = redirects;
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
        public IActionResult Get(string url)
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
                    var fuzzy = Fuzzy.Search(url, redirects.Urls.Keys.ToList());
                    var suggestions = redirects.Urls.Where(u=>fuzzy.Contains(u.Key)).ToDictionary(s=>s.Key, s=>s.Value);
                    ViewBag.Url = url;
                    return View("404", suggestions);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    return BadRequest(ex);
                }
            }
        }
    }
}

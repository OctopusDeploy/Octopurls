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

namespace Octopurls
{
    [Route("")]
    public class UrlsController : Controller
    {
        readonly Redirects redirects;
        readonly RaygunSettings raygun;

        public UrlsController(Redirects redirects, RaygunSettings raygun)
        {
            this.redirects = redirects;
            this.raygun = raygun;
        }

        [HttpGet("")]
        public IActionResult Get()
        {
            return new RedirectResult("http://www.octopusdeploy.com");
        }

        [HttpGet("{url}")]
        public async Task<IActionResult> Get(string url)
        {
            Console.WriteLine("Finding redirect for shortened URL '{0}'", url);
            try
            {
                string redirectUrl;
                if ((redirects.Urls.TryGetValue(url, out redirectUrl)))
                {
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
                    return HttpNotFound();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    return HttpBadRequest();
                }
            }
        }
    }
}

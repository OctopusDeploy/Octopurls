using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace Octopurls
{
    [Route("")]
    public class UrlsController : Controller
    {
        readonly Redirects redirects;
        public UrlsController(Redirects redirects)
        {
            this.redirects = redirects;
        }
        
        [HttpGet("")]
        public IActionResult Get()
        {
            return new RedirectResult("http://www.octopusdeploy.com");
        }

        [HttpGet("{url}")]
        public IActionResult Get(string url)
        {
            string redirectUrl;
            if ((redirects.Urls.TryGetValue(url, out redirectUrl)))
            {
                return new RedirectResult(redirectUrl);
            }
            return HttpNotFound();
        }
    }
}

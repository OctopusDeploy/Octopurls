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
        // GET: api/values
        [HttpGet]
        [Route("")]
        public IDictionary<string, string> Get()
        {
            return redirects.Urls;
        }

        // GET api/values/5
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

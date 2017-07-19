using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Xunit;
using Octopurls;
using System.Linq;

namespace Octopurls.Tests
{
    public class RedirectsFileTests
    {
        readonly Redirects redirects;
        public RedirectsFileTests()
        {
            var redirectsPath = Path.Combine("redirects.json");
            using(var redirectsFile = new StreamReader(new FileStream(redirectsPath, FileMode.Open)))
            {
                var urls = JsonConvert.DeserializeObject<Dictionary<string, string>>(redirectsFile.ReadToEnd());
                redirects = new Redirects
                {
                    Urls = new Dictionary<string, string>(urls, StringComparer.OrdinalIgnoreCase)
                };
            }
        }

        // Check that someone haven't added a redirect for `ping`
        // `ping` is used for pingdom monitoring of this app
        [Fact]
        public void CheckThatRedirectsFileDoesNotContainEntryForPing()
        {
            Assert.False(
                redirects.Urls.Keys.Where(k => k.ToLowerInvariant() == "ping").Any(),
                "Redirect file should not contain an entry for 'ping'"
            );
        }

        // Check that someone haven't added a redirect for `missing`
        // `missing` is used for customers to send feedback on how/where
        // they encountered a missing link
        [Fact]
        public void CheckThatRedirectsFileDoesNotContainEntryForMissing()
        {
            Assert.False(
                redirects.Urls.Keys.Where(k => k.ToLowerInvariant() == "feedback").Any(),
                "Redirect file should not contain an entry for 'feedback'"
            );
        }

        // Check that someone haven't added  redirect for `favicon.ico`
        // `favicon.ico` is requested when hitting the `ping` endpoint and causing the
        // missing URL Slack notification to be sent
        [Fact]
        public void CheckThatRedirectsFileDoesNotContainEntryForFavicon()
        {
            Assert.False(
                redirects.Urls.Keys.Where(k => k.ToLowerInvariant() == "favicon.ico").Any(),
                "Redirect file should not contain an entry for 'favicon.ico'"
            );
        }

        // Check that someone haven't added  redirect for `robots.txt`
        // `robots.txt` is requested by search engine web crawlers
        [Fact]
        public void CheckThatRedirectsFileDoesNotContainEntryForRobotsTxt()
        {
            Assert.False(
                redirects.Urls.Keys.Where(k => k.ToLowerInvariant() == "robots.txt").Any(),
                "Redirect file should not contain an entry for 'robots.txt'"
            );
        }
    }
}

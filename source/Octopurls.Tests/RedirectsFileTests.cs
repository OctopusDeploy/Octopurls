using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Octopurls.Tests
{
    public class RedirectsFileTests
    {
        readonly Redirects _redirects;

        public RedirectsFileTests()
        {
            var redirectsPath = Path.Combine("redirects.json");
            using(var redirectsFile = new StreamReader(new FileStream(redirectsPath, FileMode.Open)))
            {
                var urls = JsonConvert.DeserializeObject<Dictionary<string, string>>(redirectsFile.ReadToEnd());
                _redirects = new Redirects
                {
                    Urls = new Dictionary<string, string>(urls, StringComparer.OrdinalIgnoreCase)
                };
            }
        }

        // Check that someone haven't added a redirect for `ping`
        // `ping` is used for pingdom monitoring of this app
        [Test]
        public void CheckThatRedirectsFileDoesNotContainEntryForPing()
        {
            _redirects.Urls.Keys.Where(k => k.ToLowerInvariant() == "ping")
                .Should()
                .BeEmpty("Redirect file should not contain an entry for 'ping'");
        }

        // Check that someone haven't added a redirect for `missing`
        // `missing` is used for customers to send feedback on how/where
        // they encountered a missing link
        [Test]
        public void CheckThatRedirectsFileDoesNotContainEntryForFeedback()
        {
            _redirects.Urls.Keys.Where(k => k.ToLowerInvariant() == "feedback")
                .Should()
                .BeEmpty("Redirect file should not contain an entry for 'feedback'");
        }

        // Check that someone haven't added  redirect for `favicon.ico`
        // `favicon.ico` is requested when hitting the `ping` endpoint and causing the
        // missing URL Slack notification to be sent
        [Test]
        public void CheckThatRedirectsFileDoesNotContainEntryForFavicon()
        {
            _redirects.Urls.Keys.Where(k => k.ToLowerInvariant() == "favicon.ico")
                .Should()
                .BeEmpty("Redirect file should not contain an entry for 'favicon.ico'");
        }

        // Check that someone haven't added  redirect for `robots.txt`
        // `robots.txt` is requested by search engine web crawlers
        [Test]
        public void CheckThatRedirectsFileDoesNotContainEntryForRobotsTxt()
        {
            _redirects.Urls.Keys.Where(k => k.ToLowerInvariant() == "robots.txt")
                .Should()
                .BeEmpty("Redirect file should not contain an entry for 'robots.txt'");
        }
    }
}

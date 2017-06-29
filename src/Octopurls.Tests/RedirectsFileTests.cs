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
        [Fact]
        public void CheckThatRedirectsFileDoesNotContainEntryForPing()
        {
            var redirectsPath = Path.Combine("redirects.json");
            Redirects redirects;
            using(var redirectsFile = new StreamReader(new FileStream(redirectsPath, FileMode.Open)))
            {
                var urls = JsonConvert.DeserializeObject<Dictionary<string, string>>(redirectsFile.ReadToEnd());
                redirects = new Redirects
                {
                    Urls = new Dictionary<string, string>(urls, StringComparer.OrdinalIgnoreCase)
                };
            }

            Assert.True(
                redirects.Urls.Keys.Where(k => k.ToLowerInvariant() == "ping").Count() == 0,
                "Redirect file should not contain an entry for 'ping'"
            );
        }
    }
}

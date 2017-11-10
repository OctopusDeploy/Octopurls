using System.Collections.Generic;
using Xunit;
using System.Linq;

namespace Octopurls.Tests
{
    public class RedirectsFileTests : OctopurlTest
    {
        [Theory]
        [InlineData("ping")] // `ping` is used for pingdom monitoring of this app
        [InlineData("fedback")] // `feedback` is used for customers to send feedback on how/where they encountered a missing link
        [InlineData("favicon.ico")] // `favicon.ico` is requested when hitting the `ping` endpoint and causing the missing URL Slack notification to be sent
        [InlineData("robots.txt")] // `robots.txt` is requested by search engine web crawlers
        [InlineData("all")] // 'all' is an API endpoint that returns a JSON blob with all the available redirects
        public void CheckThatRedirectsFileDoesNotContainEntriesForReservedURLs(string url)
        {
            Assert.False(
                redirects.Urls.Keys.Any(k => k.ToLowerInvariant() == url),
                $"Redirect file should not contain an entry for '{url}'"
            );
        }
    }
}

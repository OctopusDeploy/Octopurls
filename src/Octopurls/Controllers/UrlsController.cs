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
using Microsoft.Extensions.Options;
using Octopurls.Models;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Octopurls
{
    [Route("")]
    public class UrlsController : Controller
    {
        readonly ILogger logger;
        readonly Redirects redirects;
        readonly SlackSettings slackSettings;
        readonly string[] urlsToIgnore = {
            "favicon.ico",
            "robots.txt"
        };

        public UrlsController(Redirects redirects, IOptions<SlackSettings> slackSettingsAccessor, ILoggerFactory logger)
        {
            this.logger = logger.CreateLogger("Octopurls.UrlsController");
            this.redirects = redirects;
            slackSettings = slackSettingsAccessor.Value;
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
            if (urlsToIgnore.Contains(url, StringComparer.OrdinalIgnoreCase)) return NoContent();

            logger.LogDebug($"Finding redirect for shortened URL '{url}' among {redirects.Urls.Count} redirects");
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
                    logger.LogDebug($"Found shortened URL '{url}' which redirects to '{redirectUrl}'");
                    return new RedirectResult(redirectUrl);
                }
                throw new KeyNotFoundException($"Could not find shortened URL '{url}' in the list of configured redirects.");
            }
            catch (KeyNotFoundException kne)
            {
                logger.LogDebug(kne, "KeyNotFoundException caught");
                try
                {
                    await SendMissingUrlNotification(url, kne, Request.Headers["Referer"]);

                    var fuzzy = Fuzzy.Search(url, redirects.Urls.Keys.ToList());
                    var suggestions = redirects.Urls.Where(u=>fuzzy.Contains(u.Key)).ToDictionary(s=>s.Key, s=>s.Value);

                    ViewBag.Url = url;
                    return View("404", suggestions);
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "An unexpected error occurred");
                    return BadRequest(ex);
                }
            }
        }

        private async Task SendMissingUrlNotification(string url, KeyNotFoundException kne, string[] referers)
        {
            if (!String.IsNullOrWhiteSpace(slackSettings.WebhookURL))
            {
                var message = new SlackRichMessage
                {
                    Attachments = (new [] {BuildSlackMessage(url, kne.Message, referers)}).ToList()
                };

                var content = JsonConvert.SerializeObject(message);

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    var result = await httpClient.PostAsync(slackSettings.WebhookURL, new StringContent(content));
                    result.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"An error occurred while sending `Missing URL Notification` to Slack: {ex.Message}");
                }
            }
        }

        private SlackMessage BuildSlackMessage(string url, string message, string[] referers)
        {
            var formattedMessage = $"Could not find shortened URL '{url}' in the list of configured redirects.";
            var fields = new List<Field> {
                new Field { Title = "Eeeek, I encountered a 404", Value = message, Short = false},
            };

            if(referers != null && referers.Any())
                fields.Add(new Field { Title = $"Referer{(referers.Count() > 1 ? "s" : "")}", Value = string.Join(",", referers), Short = false});

            fields.Add(new Field { Title = "Octopurls version", Value = GetInformationalVersion(), Short = true});
            fields.Add(new Field { Title = "Octopurls environment", Value = slackSettings.AppEnvironment, Short = true});

            return new SlackMessage
            {
                PreText = $"Oops...someone encountered the very rare Octopusaurus",
                Color = "warning",
                Fallback = message,
                Fields = fields.ToList()
            };
        }

        private string GetInformationalVersion()
        {
            return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }
    }
}

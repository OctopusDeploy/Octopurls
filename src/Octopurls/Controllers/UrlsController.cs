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

        readonly string[] webCrawlers =
        {
            "YandexBot",
            "GarlikCrawler"
        };

        public UrlsController(Redirects redirects, IOptions<SlackSettings> slackSettingsAccessor, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger("Octopurls.UrlsController");
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

        [HttpPost("feedback")]
        public async Task<IActionResult> SendFeedback(string url, string message)
        {
            await SendFeedbackNotification(url, message).ConfigureAwait(false);
            return Redirect("feedback");
        }

        [HttpGet("feedback")]
        public IActionResult ThankYou()
        {
            return View("ThankYou");
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
                    var fuzzy = Fuzzy.Search(url, redirects.Urls.Keys.ToList());
                    var suggestions = redirects.Urls.Where(u=>fuzzy.Contains(u.Key)).ToDictionary(s=>s.Key, s=>s.Value);

                    if(!suggestions.Any())
                    {
                        var userAgent = Request.Headers["User-Agent"][0];
                        if(!string.IsNullOrEmpty(userAgent) && !webCrawlers.Any(wc => userAgent.ToLowerInvariant().Contains(wc.ToLowerInvariant())))
                            await SendMissingUrlNotification(url, kne, ("Referer", Request.Headers["Referer"]), ("UserAgent", userAgent)).ConfigureAwait(false);
                    }

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

        private async Task SendFeedbackNotification(string url, string message)
        {
            if(!String.IsNullOrWhiteSpace(slackSettings.WebhookURL) && !String.IsNullOrWhiteSpace(message))
            {
                try
                {
                    var result = await SendToSlack(BuildFeedbackMessage(url, message));
                    result.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"An error occurred while sending `Feedback Notification` to Slack: {ex.Message}");
                }
            }
        }

        private SlackMessage BuildFeedbackMessage(string url, string message)
        {
            var fields = new List<Field>
            {
                new Field { Title = "The following feedback was received from the customer", Value = message, Short = false },
            };

            fields.AddRange(GetOctopurlsEnvironmentDetails());

            return new SlackMessage
            {
                PreText = $"A customer who encountered the missing shortened URL '{url}' has provided feedback on how/where they found it",
                Color = "good",
                Fallback = message,
                Fields = fields.ToList()
            };
        }

        private async Task SendMissingUrlNotification(string url, KeyNotFoundException kne, params (string title, string value)[] extraFields)
        {
            if (!String.IsNullOrWhiteSpace(slackSettings.WebhookURL))
            {
                try
                {
                    var result = await SendToSlack(BuildSlackMessage(url, kne.Message, extraFields));
                    result.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"An error occurred while sending `Missing URL Notification` to Slack: {ex.Message}");
                }
            }
        }

        private SlackMessage BuildSlackMessage(string url, string message, params (string title, string value)[] extraFields)
        {
            var fields = new List<Field> {
                new Field { Title = "Eeeek, I encountered a 404", Value = message, Short = false},
            };

            if (extraFields != null && extraFields.Any())
            {
                fields.AddRange(
                    extraFields
                        .Where(field => !string.IsNullOrEmpty(field.value))
                        .Select(field =>
                            new Field
                            {
                                Title = field.title,
                                Value = field.value,
                                Short = false
                            }
                        )
                );
            }

            fields.AddRange(GetOctopurlsEnvironmentDetails());

            return new SlackMessage
            {
                PreText = "Oops...someone encountered the very rare Octopusaurus",
                Color = "warning",
                Fallback = message,
                Fields = fields.ToList()
            };
        }

        private async Task<HttpResponseMessage> SendToSlack(params SlackMessage[] messages)
        {
            var message = new SlackRichMessage
            {
                Attachments = messages.ToList()
            };

            var content = JsonConvert.SerializeObject(message);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return await httpClient.PostAsync(slackSettings.WebhookURL, new StringContent(content));
        }

        private IEnumerable<Field> GetOctopurlsEnvironmentDetails()
        {
            return new [] {
                new Field { Title = "Octopurls version", Value = GetInformationalVersion(), Short = true},
                new Field { Title = "Octopurls environment", Value = slackSettings.AppEnvironment, Short = true}
            };
        }
        private string GetInformationalVersion()
        {
            return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }
    }
}

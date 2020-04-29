using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Octopurls.Models;

namespace Octopurls
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var redirectsPath = Path.Combine(Directory.GetCurrentDirectory(), "redirects.json");
            using (var redirectsFile = new StreamReader(new FileStream(redirectsPath, FileMode.Open)))
            {
                var urls = JsonConvert.DeserializeObject<Dictionary<string, string>>(redirectsFile.ReadToEnd());
                Redirects = new Redirects
                {
                    Urls = new Dictionary<string, string>(urls, StringComparer.OrdinalIgnoreCase)
                };
            }

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
        }

        public IConfiguration Configuration { get; set; }

        public Redirects Redirects { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddControllersWithViews();
            services.Configure<SlackSettings>(Configuration.GetSection("Slack"));
            services.Configure<WebCrawlers>(Configuration.GetSection("WebCrawlers"));
            services.Configure<IgnoredUrls>(Configuration.GetSection("IgnoredUrls"));
            services.AddSingleton(_ => Redirects);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
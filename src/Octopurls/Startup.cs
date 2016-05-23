using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Octopurls
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            Configuration = configuration;

            var redirectsPath = Path.Combine(Directory.GetCurrentDirectory(), "redirects.json");
            using(var redirectsFile = new StreamReader(new FileStream(redirectsPath, FileMode.Open)))
            {
                var urls = JsonConvert.DeserializeObject<Dictionary<string, string>>(redirectsFile.ReadToEnd());
                Redirects = new Redirects
                {
                    Urls = new Dictionary<string, string>(urls, StringComparer.OrdinalIgnoreCase)
                };
            }
        }

        public IConfiguration Configuration { get; set; }

        public Redirects Redirects {get; private set;}

        public void ConfigureServices(IServiceCollection services)
        {
            var raygunSettings = new RaygunSettings();
            raygunSettings.ApiKey = Configuration.Get("RAYGUN_APIKEY", string.Empty);
            services.AddSingleton(_ => raygunSettings);

            services.AddSingleton(_ => Redirects);
            services.AddSingleton(_ => Configuration);
            
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            
            if(env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseMvc();
        }
        
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}

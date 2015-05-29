using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Newtonsoft.Json;

namespace Octopurls
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            var configuration = new Configuration()
                .AddEnvironmentVariables();

            Configuration = configuration;

            var redirectsPath = Path.Combine(appEnv.ApplicationBasePath, "redirects.json");
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

        public void ConfigureServices(IServiceCollection services, IHostingEnvironment env)
        {
            var raygunSettings = new RaygunSettings();
            raygunSettings.ApiKey = Configuration.Get("RAYGUN_APIKEY");
            services.AddSingleton(_ => raygunSettings);

            services.AddSingleton(_ => Redirects);
            services.AddSingleton(_ => Configuration);
            
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            app.UseStaticFiles();
            app.UseMvc();
        }
    }
}

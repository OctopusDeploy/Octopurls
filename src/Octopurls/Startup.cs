using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
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
        public Redirects Redirects {get; private set;}

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_ => Redirects);
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

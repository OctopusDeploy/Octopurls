using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octopurls.Models;

namespace Octopurls.Tests
{
    public abstract class OctopurlTest
    {
        private IConfiguration configuration;

        internal Redirects redirects;

        protected OctopurlTest()
        {
            var startup = new Startup(configuration);

            redirects = startup.Redirects;
        }
    }
}


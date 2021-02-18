using System.Reflection;
using FluentAssertions;
using NUnit.Framework;

namespace Octopurls.Tests
{
    public class VersionTests
    {
        [Test]
        public void MainlineVersioningIsUsed()
        {
            typeof(Startup).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                .Should()
                .NotContain("-ci", "Builds from `master` should have non-prerelease version numbers so that it gets deployed to production automatically.");
        }
    }
}
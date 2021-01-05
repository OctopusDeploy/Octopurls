//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=xunit.runner.console";
#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0
#tool "nuget:?package=TeamCity.Dotnet.Integration&version=1.0.10"
#addin "nuget:?package=Cake.OctoVersion&version=0.0.138"

// The list below is to manually resolve NuGet dependencies to work around a bug in Cake's dependency loader.
// Our intention is to remove this list again once the Cake bug is fixed.
//
// What we want:
// #addin "nuget:?package=Cake.OctoVersion&version=0.0.138&loaddependencies=true"
// (Note the loaddependencies=true parameter.)
//
// Our workaround:
#addin "nuget:?package=LibGit2Sharp&version=0.26.2"
#addin "nuget:?package=Serilog&version=2.8.0.0"
#addin "nuget:?package=Serilog.Settings.Configuration&version=3.1.0.0"
#addin "nuget:?package=Serilog.Sinks.Console&version=3.0.1.0"
#addin "nuget:?package=Serilog.Sinks.Literate&version=3.0.0.0"
#addin "nuget:?package=SerilogMetrics&version=2.1.0.0"
#addin "nuget:?package=OctoVersion.Core&version=0.0.138"
#addin "nuget:?package=Cake.OctoVersion&version=0.0.138"
#addin "nuget:?package=Microsoft.Extensions.Primitives&version=3.1.7"
#addin "nuget:?package=Microsoft.Extensions.Configuration&version=3.1.7.0"
#addin "nuget:?package=Microsoft.Extensions.Configuration.Abstractions&version=3.1.7.0"
#addin "nuget:?package=Microsoft.Extensions.Configuration.Binder&version=3.1.7.0"
#addin "nuget:?package=Microsoft.Extensions.Configuration.CommandLine&version=3.1.7.0"
#addin "nuget:?package=Microsoft.Extensions.Configuration.EnvironmentVariables&version=3.1.7.0"
#addin "nuget:?package=Microsoft.Extensions.Configuration.FileExtensions&version=3.1.0.0"
#addin "nuget:?package=Microsoft.Extensions.Configuration.Json&version=3.1.7"
#addin "nuget:?package=Microsoft.Extensions.DependencyModel&version=2.0.4.0"
#addin "nuget:?package=Microsoft.Extensions.FileProviders.Abstractions&version=3.1.0.0"
#addin "nuget:?package=Microsoft.Extensions.FileProviders.Physical&version=3.1.0.0"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
//////////////////////////////////////////////////////////////////////
var publishDir = "./publish";
var artifactsDir = "./artifacts";
var projectToPublish = "./src/Octopurls/Octopurls.csproj";
var projectName = "Octopurls";

if (!BuildSystem.IsRunningOnTeamCity) OctoVersionDiscoverLocalGitBranch(out _);
OctoVersion(out var versionInfo);

//////////////////////////////////////////////////////////////////////
// SETUP /TEARDOWN
//////////////////////////////////////////////////////////////////////
Setup(context =>
{
    if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(versionInfo.FullSemVer);
    
    Information($"Building {projectName} v{versionInfo.FullSemVer}");
});

Teardown(context =>
{
    Information($"Finished running task for build v{versionInfo.FullSemVer}");
});

//////////////////////////////////////////////////////////////////////
// PRIVATE TASKS
//////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() => {
        CleanDirectory(publishDir);
        CleanDirectory(artifactsDir);
        CleanDirectories("./src/**/bin");
        CleanDirectories("./src/**/obj");
        CleanDirectories("./src/**/TestResults");
    });


Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => DotNetCoreRestore("src"));

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        DotNetCoreBuild("./src", new DotNetCoreBuildSettings
        {
            Configuration = configuration,
            ArgumentCustomization = args => args
                .Append($"/p:Version={versionInfo.FullSemVer}")
                .Append($"/p:InformationalVersion={versionInfo.FullSemVer}")
                .Append("--verbosity normal")
        });
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        GetFiles("./src/Octopurls.Tests/Octopurls.Tests.csproj")
            .ToList()
            .ForEach(testProjectFile =>
            {
                DotNetCoreTest(testProjectFile.FullPath, new DotNetCoreTestSettings
                {
                    Configuration = configuration,
                    NoBuild = true,
                    ArgumentCustomization = args => args
                        .Append("-l trx")
                        .Append("--verbosity normal")
                });
            });
    });

Task("DotNetCorePublish")
    .IsDependentOn("Test")
    .Does(() =>
    {
        DotNetCorePublish(projectToPublish, new DotNetCorePublishSettings
        {
            Framework = "netcoreapp3.1",
            Configuration = configuration,
            OutputDirectory = publishDir,
            ArgumentCustomization = args => args
                .Append($"/p:Version={versionInfo.FullSemVer}")
                .Append($"/p:InformationalVersion={versionInfo.FullSemVer}")
                .Append("--verbosity normal")
        });
    });

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("DotNetCorePublish");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
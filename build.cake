//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=xunit.runner.console";
#addin "nuget:?package=Newtonsoft.Json";
using Newtonsoft.Json;

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

string buildNumber;

//////////////////////////////////////////////////////////////////////
// SETUP /TEARDOWN
//////////////////////////////////////////////////////////////////////
Setup(context =>
{
    buildNumber = EnvironmentVariable("BUILD_NUMBER");

    Information($"Building {projectName} v{buildNumber}");
});

Teardown(context =>
{
    Information($"Finished running task for build v{buildNumber}");
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
                .Append($"/p:Version={buildNumber}")
                .Append($"/p:InformationalVersion={buildNumber}")
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
                .Append($"/p:Version={buildNumber}")
                .Append($"/p:InformationalVersion={buildNumber}")
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
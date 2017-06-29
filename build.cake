//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine";
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

GitVersion gitVersionInfo;
string nugetVersion;

//////////////////////////////////////////////////////////////////////
// SETUP /TEARDOWN
//////////////////////////////////////////////////////////////////////
Setup(context =>
{
    gitVersionInfo = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json
    });
    nugetVersion = gitVersionInfo.NuGetVersion;

    Information("Output from GitVersion:");
    Information(JsonConvert.SerializeObject(gitVersionInfo, Formatting.Indented));

    if (BuildSystem.IsRunningOnTeamCity) {
        BuildSystem.TeamCity.SetBuildNumber(nugetVersion);
    }

    Information($"Building {projectName} v{nugetVersion}");
    Information($"Informational Version {gitVersionInfo.InformationalVersion}");
});

Teardown(context =>
{
    Information($"Finished running task for build v{nugetVersion}");
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
            ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
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
                    ArgumentCustomization = args => args.Append("-l trx")
                });
            });
    });

Task("DotNetCorePublish")
    .IsDependentOn("Test")
    .Does(() =>
    {
        DotNetCorePublish(projectToPublish, new DotNetCorePublishSettings
        {
            Configuration = configuration,
            OutputDirectory = publishDir
        });
    });

Task("Zip")
    .IsDependentOn("DotNetCorePublish")
    .Does(() =>
    {
        Zip(publishDir, $"{artifactsDir}/{projectName}.{nugetVersion}.zip");
    });

Task("Publish")
    .IsDependentOn("Zip")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() => {
        UploadFile(
            $"{EnvironmentVariable("Octopus3ServerUrl")}/api/packages/raw?apiKey={EnvironmentVariable("Octopus3ApiKey")}",
            $"{artifactsDir}/{projectName}.{nugetVersion}.zip"
        );
    });

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("Publish");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    [Parameter("ApiKey for the specified source.")] readonly string ApiKey;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    string Source => "https://api.nuget.org/v3/index.json";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath SrcDirectory => RootDirectory / "src";


    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.GetNormalizedAssemblyVersion())
                .SetFileVersion(GitVersion.GetNormalizedFileVersion())
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        });
    
    
    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(s => s 
                .SetProject(Solution.GetProject("Bert.RateLimiters"))
                .SetAuthors("Robert Mircea")
                .SetPackageId("Bert.RateLimiters")
                .SetPackageLicenseUrl("https://raw.githubusercontent.com/robertmircea/RateLimiters/master/LICENSE")
                .SetPackageProjectUrl("https://github.com/robertmircea/RateLimiters")
                .SetDescription("Popular rate limiting algorithms. C# implementations of fixed token bucket and leaky token bucket throttling strategies")
                .SetPackageReleaseNotes("Updated to .NETStandard")
                .SetCopyright("2014-2019")
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableIncludeSymbols()
                .SetVersion(GitVersion.NuGetVersionV2)
                .SetOutputDirectory(OutputDirectory)
                .SetPackageRequireLicenseAcceptance(false)
                .SetPackageTags("ratelimiting","throttling","leakybucket","tokenbucket")
            );
        });

    Target Publish => _ => _
        .DependsOn(Pack)
        .Requires(() => ApiKey)
        .Requires(() => Configuration.Release)
        .Executes(() =>
        {
            GlobFiles(OutputDirectory, "*.nupkg").NotEmpty()
                .Where(x => !x.EndsWith(".symbols.nupkg"))
                .ForEach(x => DotNetNuGetPush(s => s
                    .SetTargetPath(x)
                    .SetSource(Source)
                    .SetApiKey(ApiKey)));
        });
}

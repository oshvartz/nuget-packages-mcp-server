using CommandLine;

namespace NugetPackagesMcpServer
{
    // Shared options for all commands
    public class CommonOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option("feed-url", Required = false, HelpText = "Override the NuGet feed URL.")]
        public string? FeedUrl { get; set; }
    }

    [Verb("versions", HelpText = "Get all available versions of a NuGet package from the configured feed.")]
    public class GetVersionsOptions : CommonOptions
    {
        [Value(0, MetaName = "packageName", Required = true, HelpText = "NuGet package name.")]
        public string PackageName { get; set; } = string.Empty;

        [Option("prerelease", Required = false, HelpText = "Include prerelease versions.")]
        public bool Prerelease { get; set; }
    }

    [Verb("dependencies", HelpText = "Get package dependencies for a specific package name and version.")]
    public class GetDependenciesOptions : CommonOptions
    {
        [Value(0, MetaName = "packageName", Required = true, HelpText = "NuGet package name.")]
        public string PackageName { get; set; } = string.Empty;

        [Value(1, MetaName = "version", Required = true, HelpText = "Package version.")]
        public string Version { get; set; } = string.Empty;
    }

    [Verb("contracts", HelpText = "Get public interfaces and classes contracts from a NuGet package as markdown.")]
    public class GetContractsOptions : CommonOptions
    {
        [Value(0, MetaName = "packageName", Required = true, HelpText = "NuGet package name.")]
        public string PackageName { get; set; } = string.Empty;

        [Value(1, MetaName = "version", Required = true, HelpText = "Package version.")]
        public string Version { get; set; } = string.Empty;
    }
}

namespace NugetPackagesMcpServer.Models
{
    public record NugetDependency
    {
        public string PackageName { get; init; } = string.Empty;
        public string VersionRange { get; init; } = string.Empty;
        public string TargetFramework { get; init; } = string.Empty;
    }
}

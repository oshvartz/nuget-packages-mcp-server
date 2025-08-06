namespace NugetPackagesMcpServer.Models
{
    public record NugetPackageVersion
    {
        public string Version { get; init; } = string.Empty;
        public bool IsPrerelease { get; init; }
    }
}

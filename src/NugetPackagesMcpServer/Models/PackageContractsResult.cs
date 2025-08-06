namespace NugetPackagesMcpServer.Models
{
    public record PackageContractsResult
    {
        public string PackageName { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Authors { get; init; } = string.Empty;
        public string Tags { get; init; } = string.Empty;
        public string License { get; init; } = string.Empty;
        public string ProjectUrl { get; init; } = string.Empty;
        public string ContractsMarkdown { get; init; } = string.Empty;
    }
}

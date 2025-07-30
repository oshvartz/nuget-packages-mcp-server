namespace NugetPackagesMcpServer.Models
{
    public class PackageContractsResult
    {
        public string PackageName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public string ProjectUrl { get; set; } = string.Empty;
        public string ContractsMarkdown { get; set; } = string.Empty;
    }
}

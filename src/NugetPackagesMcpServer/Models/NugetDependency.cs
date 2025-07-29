namespace NugetPackagesMcpServer.Models
{
    public class NugetDependency
    {
        public string PackageName { get; set; } = string.Empty;
        public string VersionRange { get; set; } = string.Empty;
        public string TargetFramework { get; set; } = string.Empty;
    }
}

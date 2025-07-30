namespace NugetPackagesMcpServer.Models
{
    public class NugetPackageVersion
    {
        public string Version { get; set; } = string.Empty;
        public bool IsPrerelease { get; set; }
    }
}

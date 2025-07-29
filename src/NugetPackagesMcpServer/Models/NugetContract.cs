namespace NugetPackagesMcpServer.Models
{
    public class NugetContract
    {
        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "interface", "class", etc.
        public List<string> Members { get; set; } = new();
    }
}

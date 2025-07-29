using System.ComponentModel.DataAnnotations;

namespace NugetPackagesMcpServer.Configuration
{
    public record NugetFeedOptions
    {
        public const string SectionName = "NugetFeed";

        [Required]
        public string? FeedUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public bool AllowPrerelease { get; set; } = false;
    }
}

using System.ComponentModel;
using Microsoft.Extensions.Options;
using NugetPackagesMcpServer.Models;
using NugetPackagesMcpServer.Services;
using NugetPackagesMcpServer.Configuration;
using ModelContextProtocol.Server;
using ModelContextProtocol;

namespace NugetPackagesMcpServer
{
    [McpServerToolType]
    public class NugetPackagesTool
    {
        private readonly INugetClientService _nugetClientService;
        private readonly NugetFeedOptions _options;

        public NugetPackagesTool(INugetClientService nugetClientService, IOptions<NugetFeedOptions> options)
        {
            _nugetClientService = nugetClientService;
            _options = options.Value;
        }

        [McpServerTool, Description("Get all available versions of a NuGet package from the configured feed")]
        public async Task<IEnumerable<NugetPackageVersion>> GetPackageVersions(string packageName, bool? includePrerelease = null)
        {
            var usePrerelease = includePrerelease ?? _options.AllowPrerelease;
            return await _nugetClientService.GetPackageVersionsAsync(packageName, usePrerelease);
        }

        [McpServerTool, Description("Get package dependencies for a specific package name and version")]
        public async Task<IEnumerable<NugetDependency>> GetPackageDependencies(string packageName, string version)
        {
            return await _nugetClientService.GetPackageDependenciesAsync(packageName, version);
        }

[McpServerTool, Description("Get public interfaces and classes contracts from a NuGet package as markdown")]
        public async Task<string> GetPackageContracts(string packageName, string version)
        {
            var result = await _nugetClientService.GetPackageContractsAsync(packageName, version);

            var md = $"# Package: {result.PackageName} v{result.Version}\n\n";
            if (!string.IsNullOrWhiteSpace(result.Description))
                md += $"## Description\n{result.Description}\n\n";
            if (!string.IsNullOrWhiteSpace(result.Authors))
                md += $"**Authors:** {result.Authors}\n\n";
            if (!string.IsNullOrWhiteSpace(result.Tags))
                md += $"**Tags:** {result.Tags}\n\n";
            if (!string.IsNullOrWhiteSpace(result.License))
                md += $"**License:** {result.License}\n\n";
            if (!string.IsNullOrWhiteSpace(result.ProjectUrl))
                md += $"**Project URL:** {result.ProjectUrl}\n\n";
            if (!string.IsNullOrWhiteSpace(result.ContractsMarkdown))
                md += $"---\n\n{result.ContractsMarkdown}\n";

            return md.Trim();
        }
    }
}

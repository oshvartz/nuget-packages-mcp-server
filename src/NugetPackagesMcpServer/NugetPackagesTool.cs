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
            var contracts = await _nugetClientService.GetPackageContractsAsync(packageName, version);
            // Convert contracts to markdown
            var md = new System.Text.StringBuilder();
            foreach (var contract in contracts)
            {
                md.AppendLine($"### {contract.Type} {contract.Name}");
                md.AppendLine($"Namespace: `{contract.Namespace}`");
                md.AppendLine();
                if (contract.Members.Any())
                {
                    md.AppendLine("Members:");
                    foreach (var member in contract.Members)
                    {
                        md.AppendLine($"- `{member}`");
                    }
                    md.AppendLine();
                }
            }
            return md.ToString();
        }
    }
}

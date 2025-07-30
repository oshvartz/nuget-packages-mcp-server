using NugetPackagesMcpServer.Models;

namespace NugetPackagesMcpServer.Services
{
    public interface INugetClientService
    {
        Task<IEnumerable<NugetPackageVersion>> GetPackageVersionsAsync(string packageName, bool includePrerelease = false);
        Task<IEnumerable<NugetDependency>> GetPackageDependenciesAsync(string packageName, string version);
        Task<PackageContractsResult> GetPackageContractsAsync(string packageName, string version);
    }
}

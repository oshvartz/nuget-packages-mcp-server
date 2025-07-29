using NugetPackagesMcpServer.Models;
using NugetPackagesMcpServer.Configuration;
using Microsoft.Extensions.Options;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol;
using NuGet.Configuration;

namespace NugetPackagesMcpServer.Services
{
    public class NugetClientService : INugetClientService
    {
        private readonly SourceRepository _repository;
        private readonly SourceCacheContext _cacheContext;

        public NugetClientService(IOptions<NugetFeedOptions> options)
        {
            // var source = new PackageSource(options.Value.FeedUrl);
            _repository = Repository.Factory.GetCoreV3(options.Value.FeedUrl);
            _cacheContext = new SourceCacheContext();
        }

        public async Task<IEnumerable<NugetPackageVersion>> GetPackageVersionsAsync(string packageName, bool includePrerelease = false)
        {
            var metadataResource = await _repository.GetResourceAsync<PackageMetadataResource>();
            var logger = NuGet.Common.NullLogger.Instance;

            var metadata = await metadataResource.GetMetadataAsync(
                packageName,
                includePrerelease,
                false,
                _cacheContext,
                logger,
                CancellationToken.None);

            var versions = metadata
                .OrderByDescending(m => m.Published)
                .Select(m => new NugetPackageVersion
                {
                    Version = m.Identity.Version.ToString(),
                    IsPrerelease = m.Identity.Version.IsPrerelease,
                    Published = m.Published?.UtcDateTime ?? DateTime.MinValue
                })
                .ToList();

            return versions;
        }

        public async Task<IEnumerable<NugetDependency>> GetPackageDependenciesAsync(string packageName, string version)
        {
            var metadataResource = await _repository.GetResourceAsync<PackageMetadataResource>();
            var logger = NuGet.Common.NullLogger.Instance;
            var identity = new NuGet.Packaging.Core.PackageIdentity(packageName, new NuGet.Versioning.NuGetVersion(version));

            var metadata = await metadataResource.GetMetadataAsync(
                identity,
                _cacheContext,
                logger,
                CancellationToken.None);

            var dependencies = new List<NugetDependency>();

            if (metadata != null)
            {
                foreach (var group in metadata.DependencySets)
                {
                    var targetFramework = group.TargetFramework.GetShortFolderName();
                    foreach (var dep in group.Packages)
                    {
                        dependencies.Add(new NugetDependency
                        {
                            PackageName = dep.Id,
                            VersionRange = dep.VersionRange?.ToString() ?? "",
                            TargetFramework = targetFramework
                        });
                    }
                }
            }

            return dependencies;
        }

        public Task<IEnumerable<NugetContract>> GetPackageContractsAsync(string packageName, string version)
        {
            // Stub: return mock data
            var contracts = new List<NugetContract>
            {
                new NugetContract
                {
                    Namespace = "Sample.Namespace",
                    Name = "IMyService",
                    Type = "interface",
                    Members = new List<string> { "void DoWork();", "int GetValue();" }
                },
                new NugetContract
                {
                    Namespace = "Sample.Namespace",
                    Name = "MyService",
                    Type = "class",
                    Members = new List<string> { "public void DoWork()", "public int GetValue()" }
                }
            };
            return Task.FromResult<IEnumerable<NugetContract>>(contracts);
        }
    }
}

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

        public async Task<PackageContractsResult> GetPackageContractsAsync(string packageName, string version)
        {
            var metadataResource = await _repository.GetResourceAsync<PackageMetadataResource>();
            var logger = NuGet.Common.NullLogger.Instance;
            var identity = new NuGet.Packaging.Core.PackageIdentity(packageName, new NuGet.Versioning.NuGetVersion(version));

            var metadata = await metadataResource.GetMetadataAsync(
                identity,
                _cacheContext,
                logger,
                CancellationToken.None);

            var result = new PackageContractsResult
            {
                PackageName = packageName,
                Version = version,
                Description = metadata?.Description ?? string.Empty,
                Authors = metadata?.Authors ?? string.Empty,
                Tags = metadata?.Tags ?? string.Empty,
                License = metadata?.LicenseMetadata?.License ?? metadata?.LicenseUrl?.ToString() ?? string.Empty,
                ProjectUrl = metadata?.ProjectUrl?.ToString() ?? string.Empty,
                ContractsMarkdown = string.Empty // To be populated separately
            };
            return result;
        }
    }
}

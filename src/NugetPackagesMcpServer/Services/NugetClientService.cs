using Microsoft.Extensions.Options;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NugetPackagesMcpServer.Configuration;
using NugetPackagesMcpServer.Models;
using PublicApiGenerator;
using System.Reflection;

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

            using MemoryStream packageStream = new MemoryStream();

            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
            await resource.CopyNupkgToStreamAsync(
                packageName,
                new NuGetVersion(version),
                packageStream,
                _cacheContext,
                logger,
                CancellationToken.None);

            using PackageArchiveReader packageReader = new PackageArchiveReader(packageStream);
            NuspecReader nuspecReader = await packageReader.GetNuspecReaderAsync(CancellationToken.None);

            // get all DLLs in the package search .netX or standard
            var dllFile = packageReader.GetFiles().Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).First();

            var outputDir = Path.Combine(Path.GetTempPath(), "extracted_dlls");
            var outputPath = Path.Combine(outputDir, Path.GetFileName(dllFile));
            Directory.CreateDirectory(outputDir);

            // Extract DLL to a stream or file
            using (var dllStream = packageReader.GetStream(dllFile))
            {
                using (var fileStream = File.Create(outputPath))
                {
                    dllStream.CopyTo(fileStream);
                }

            }
            var asm = Assembly.LoadFrom(outputPath);

            var contractsMarkdown = asm.GeneratePublicApi();


            var result = new PackageContractsResult
            {
                PackageName = packageName,
                Version = version,
                Description = nuspecReader.GetDescription() ?? string.Empty,
                Authors = nuspecReader.GetAuthors() ?? string.Empty,
                Tags = nuspecReader.GetTags() ?? string.Empty,
                ProjectUrl = nuspecReader.GetProjectUrl() ?? string.Empty,
                ContractsMarkdown = contractsMarkdown
            };
            return result;
        }
    }
}

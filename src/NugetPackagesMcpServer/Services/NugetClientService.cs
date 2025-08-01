using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NugetPackagesMcpServer.Configuration;
using NugetPackagesMcpServer.Models;
using PublicApiGenerator;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace NugetPackagesMcpServer.Services
{
    public class NugetClientService : INugetClientService
    {
        private readonly SourceRepository _repository;
        private readonly SourceCacheContext _cacheContext;

        public NugetClientService(IOptions<NugetFeedOptions> options)
        {
            _repository = CreateSourceRepository(options.Value);
            _cacheContext = new SourceCacheContext();
        }

        private SourceRepository CreateSourceRepository(NugetFeedOptions options)
        {
            if (options.UseDefaultAzureCredential &&
                (options.FeedUrl?.Contains("pkgs.dev.azure.com", StringComparison.OrdinalIgnoreCase) == true ||
                 options.FeedUrl?.Contains("visualstudio.com", StringComparison.OrdinalIgnoreCase) == true))
            {
                var credential = new DefaultAzureCredential();
                var tokenRequestContext = new TokenRequestContext(new[] { "499b84ac-1321-427f-aa17-267ca6975798/.default" });
                var token = credential.GetToken(tokenRequestContext, CancellationToken.None);

                var source = new PackageSource(options.FeedUrl)
                {
                    Credentials = new PackageSourceCredential(
                        options.FeedUrl,
                        "AzureDevOps",
                        token.Token,
                        isPasswordClearText: true,
                        "basic")
                };
                return Repository.Factory.GetCoreV3(source);
            }

            return Repository.Factory.GetCoreV3(options.FeedUrl);
        }

        public async Task<IEnumerable<NugetPackageVersion>> GetPackageVersionsAsync(string packageName, bool includePrerelease = false, int top = 10)
        {
            FindPackageByIdResource resource = await _repository.GetResourceAsync<FindPackageByIdResource>();

            var packagesVersions = await resource.GetAllVersionsAsync(
                packageName,
                _cacheContext,
                NuGet.Common.NullLogger.Instance,
                CancellationToken.None);
        

            var metadataResource = await _repository.GetResourceAsync<PackageMetadataResource>();
            var logger = NuGet.Common.NullLogger.Instance;

            IEnumerable<IPackageSearchMetadata>? metadata = await metadataResource.GetMetadataAsync(
                packageName,
                includePrerelease,
                false,
                _cacheContext,
                logger,
                CancellationToken.None);

            var versions = packagesVersions
                .Where(p => !p.IsPrerelease || includePrerelease)
                .OrderByDescending(m => m.Version)
                .Select(m => new NugetPackageVersion
                {
                    Version = m.Version.ToString(),
                    IsPrerelease = m.IsPrerelease
                })
                .Take(top)
                .ToList();

            return versions;
        }

        public async Task<IEnumerable<NugetDependency>> GetPackageDependenciesAsync(string packageName, string version)
        {
            FindPackageByIdResource? resource = await _repository.GetResourceAsync<FindPackageByIdResource>();
            var identity = new NuGet.Packaging.Core.PackageIdentity(packageName, new NuGet.Versioning.NuGetVersion(version));

            var dependencyInfo = await resource.GetDependencyInfoAsync(
                packageName,
                new NuGet.Versioning.NuGetVersion(version),
                _cacheContext,
                NuGet.Common.NullLogger.Instance,
                CancellationToken.None);

            var dependencies = new List<NugetDependency>();

            if (dependencyInfo != null)
            {
                foreach (var group in dependencyInfo.DependencyGroups)
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

            Regex regex = new Regex("net\\d.0|netstandard");

            // get all DLLs in the package search .netX or standard
            var dllFile = packageReader.GetFiles().Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .First(f => regex.IsMatch(f));

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

            var asm = Assembly.LoadFile(outputPath);

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

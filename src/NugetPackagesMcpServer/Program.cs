using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NugetPackagesMcpServer;
using NugetPackagesMcpServer.Configuration;
using NugetPackagesMcpServer.Services;
using Serilog;
using Serilog.Events;

namespace NugetPackagesMcpServer;

public class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            // Run as MCP server when no arguments provided
            await RunMcpServerMode();
        }
        else
        {
            await RunCliModeAsync(args);
        }
    }

    private static async Task RunMcpServerMode()
    {
        try
        {
            var builder = Host.CreateApplicationBuilder();

            // Get the directory where the DLL is located
            var currentDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var logPath = Path.Combine(currentDir ?? "", "server.log");

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    standardErrorFromLevel: LogEventLevel.Error)
                .WriteTo.File(logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 5,
                    fileSizeLimitBytes: 10 * 1024 * 1024) // 10MB size limit
                .CreateLogger();

            // Configure builder to use Serilog
            builder.Logging.ClearProviders();
            builder.Services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));
            builder.Services.AddOptions<NugetFeedOptions>()
            .BindConfiguration(NugetFeedOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();


            var logger = Log.Logger;
            logger.Information("Starting MCP Server initialization...");

            logger.Debug("Registering services...");
            // Register NugetClientService as singleton
            builder.Services.AddSingleton<INugetClientService, NugetClientService>();
            builder.Services.AddSingleton<IAssemblyContractResolver, AssemblyContractResolver>();

            logger.Debug("Configuring MCP server...");
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            logger.Information("Building and starting MCP server...");
            var host = builder.Build();

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                logger.Fatal(eventArgs.ExceptionObject as Exception, "Unhandled exception occurred");
            };

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Logger.Fatal(ex, "MCP Server failed to start");
            Log.Logger.Error("Exception details: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                Log.Logger.Error("Inner exception: {Message}", ex.InnerException.Message);
            }
            Log.Logger.Debug("Stack trace: {StackTrace}", ex.StackTrace);
            Log.CloseAndFlush();

            Environment.Exit(1);
        }
    }

    private static async Task RunCliModeAsync(string[] args)
    {
        // Setup DI for CLI mode
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Configure<NugetPackagesMcpServer.Configuration.NugetFeedOptions>(
            builder.Configuration.GetSection(NugetPackagesMcpServer.Configuration.NugetFeedOptions.SectionName));
        builder.Services.AddSingleton<NugetPackagesMcpServer.Services.INugetClientService, NugetPackagesMcpServer.Services.NugetClientService>();
        builder.Services.AddSingleton<IAssemblyContractResolver, AssemblyContractResolver>();

        using var host = builder.Build();
        var serviceProvider = host.Services;

        await Parser.Default.ParseArguments<GetVersionsOptions, GetDependenciesOptions, GetContractsOptions>(args)
            .MapResult(
                async (GetVersionsOptions opts) =>
                {
                    var svc = serviceProvider.GetRequiredService<NugetPackagesMcpServer.Services.INugetClientService>();
                    var versions = await svc.GetPackageVersionsAsync(opts.PackageName, opts.Prerelease);
                    foreach (var v in versions)
                    {
                        Console.WriteLine(v.Version);
                    }
                    return 0;
                },
                async (GetDependenciesOptions opts) =>
                {
                    var svc = serviceProvider.GetRequiredService<NugetPackagesMcpServer.Services.INugetClientService>();
                    var deps = await svc.GetPackageDependenciesAsync(opts.PackageName, opts.Version);
                    foreach (var d in deps)
                    {
                        Console.WriteLine($"{d.PackageName} {d.VersionRange} {d.TargetFramework}");
                    }
                    return 0;
                },
                async (GetContractsOptions opts) =>
                {
                    var svc = serviceProvider.GetRequiredService<NugetPackagesMcpServer.Services.INugetClientService>();
                    var md = await svc.GetPackageContractsAsync(opts.PackageName, opts.Version);
                    Console.WriteLine(md);
                    return 0;
                },
                errs => Task.FromResult(1)
            );
    }
}

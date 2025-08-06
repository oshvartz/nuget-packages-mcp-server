using System.Reflection;

namespace NugetPackagesMcpServer.Services
{
    public interface IAssemblyContractResolver
    {
        string ResolveAssemblyContractToMarkdown(Assembly assembly);
    }
}

# NuGet Packages MCP Server

A Model Context Protocol (MCP) server that provides tools for searching and retrieving NuGet package information. This server enables AI assistants to interact with NuGet package repositories through standardized MCP tools.

## Overview

The NuGet Packages MCP Server is a .NET-based implementation that exposes NuGet package metadata retrieval capabilities through the MCP protocol. It allows AI assistants to:

- Retrieve all available versions of a NuGet package
- Analyze package dependencies for specific versions
- Extract and document public API contracts from packages
- Access package information from any NuGet v3 feed

## Features

- **Version Discovery**: Get all available versions of a package including prerelease versions
- **Dependency Analysis**: View package dependencies and their version requirements for specific package versions
- **API Contract Extraction**: Generate markdown documentation of public interfaces and classes from packages
- **Package Metadata**: Access detailed package information including authors, license, tags, and project URLs
- **Flexible Feed Support**: Configure to use any NuGet v3 compatible feed (default: nuget.org)
- **MCP Protocol Compliance**: Full support for MCP tool schemas and communication

## Prerequisites

- .NET 8.0 SDK or later
- Node.js (for running the MCP Inspector)
- An MCP-compatible client (e.g., Claude Desktop, Cline VS Code extension)

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/oshvartz/nuget-packages-mcp-server.git
   cd nuget-packages-mcp-server
   ```

2. Build the project:
   ```bash
   dotnet build -c Release
   ```

3. The built DLL will be located at:
   ```
   src/NugetPackagesMcpServer/bin/Release/net8.0/NugetPackagesMcpServer.dll
   ```

## Configuration

### Adding to MCP Settings

Add the following configuration to your MCP client settings (e.g., `cline_mcp_settings.json`):

```json
{
  "nuget-packages-mcp-server": {
    "disabled": false,
    "timeout": 300,
    "command": "dotnet",
    "args": [
      "C:/path/to/NugetPackagesMcpServer.dll"
    ],
    "env": {
      "NugetFeed:FeedUrl": "https://api.nuget.org/v3/index.json"
    },
    "type": "stdio"
  }
}
```

### Configuration Options

- **timeout**: Maximum time (in seconds) for server operations
- **NugetFeed:FeedUrl**: The NuGet v3 feed URL to use (defaults to nuget.org)

## Available Tools

The server provides the following MCP tools:

### GetPackageVersions
Get all available versions of a NuGet package from the configured feed.

**Parameters:**
- `packageName` (string, required): The name of the NuGet package
- `includePrerelease` (boolean, optional): Include prerelease versions in results (defaults to feed configuration)

### GetPackageDependencies
Get package dependencies for a specific package name and version.

**Parameters:**
- `packageName` (string, required): The name of the NuGet package
- `version` (string, required): The specific version of the package

### GetPackageContracts
Get public interfaces and classes contracts from a NuGet package as markdown. This tool extracts and documents the public API surface of a package.

**Parameters:**
- `packageName` (string, required): The name of the NuGet package
- `version` (string, required): The specific version of the package

**Returns:** A markdown document containing:
- Package metadata (description, authors, tags, license, project URL)
- Public interfaces and classes with their members
- Method signatures and documentation

## Testing with MCP Inspector

The repository includes a PowerShell script (`test-mcp-inspector.ps1`) that uses the official MCP Inspector tool to test and debug the server.

### What is MCP Inspector?

MCP Inspector is an official debugging tool from the Model Context Protocol team that provides:
- A web-based interface for testing MCP servers
- Interactive tool invocation and response inspection
- Schema validation and protocol compliance checking
- Real-time server communication monitoring

### Running the Inspector

1. Ensure prerequisites are installed:
   - .NET 8.0 SDK
   - Node.js and npm

2. Run the test script:
   ```powershell
   .\test-mcp-inspector.ps1
   ```

3. The script will:
   - Verify all prerequisites
   - Build the project if needed
   - Start the MCP Inspector on http://localhost:3000
   - Launch the NuGet MCP server for testing

### Test Script Options

```powershell
# Use default settings (port 3000, Release build)
.\test-mcp-inspector.ps1

# Use custom port
.\test-mcp-inspector.ps1 -Port 3001

# Use Debug build
.\test-mcp-inspector.ps1 -ServerDll "./src/NugetPackagesMcpServer/bin/Debug/net8.0/NugetPackagesMcpServer.dll"
```

### Using the Inspector

Once running, the MCP Inspector allows you to:

1. **View Available Tools**: See all tools exposed by the server with their schemas
2. **Test Tool Invocations**: 
   - Select a tool (e.g., `GetPackageVersions`)
   - Fill in parameters
   - Execute and see the response
3. **Inspect Communication**: View the raw MCP protocol messages
4. **Validate Responses**: Ensure responses match expected schemas

Example test scenarios:
- Get all versions of popular packages like "Newtonsoft.Json"
- Check dependencies for a specific version (e.g., "Newtonsoft.Json" version "13.0.3")
- Extract public API contracts from a package
- Test with and without prerelease versions

## Development

### Project Structure

```
nuget-packages-mcp-server/
├── src/
│   └── NugetPackagesMcpServer/
│       ├── Configuration/      # Configuration models
│       ├── Models/            # Data models
│       ├── Services/          # NuGet client services
│       ├── Program.cs         # Entry point
│       └── NugetPackagesTool.cs  # MCP tool implementation
├── test-mcp-inspector.ps1     # Testing script
└── README.md
```

### Building from Source

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Run tests (if available)
dotnet test
```

## Troubleshooting

### Common Issues

1. **Server fails to start**: 
   - Ensure .NET 8.0 SDK is installed
   - Check the DLL path in your configuration

2. **Tool execution fails**:
   - Verify the NuGet feed URL is correct
   - Check network connectivity
   - Ensure the package name and version are valid
   - Try with a known package like "Newtonsoft.Json"

3. **MCP Inspector issues**:
   - Ensure Node.js is installed
   - Check if port 3000 is available
   - Try running with elevated permissions if needed

### Debug Logging

The server supports standard .NET logging. Set the logging level in your environment:

```bash
set LOGGING__LOGLEVEL__DEFAULT=Debug
```

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

[Add your license information here]

## Acknowledgments

- Built using the [Model Context Protocol](https://modelcontextprotocol.io/)
- Powered by [NuGet Client SDK](https://github.com/NuGet/NuGet.Client)

# PowerShell
<#
.SYNOPSIS
    Test script for Nuget Packages MCP Server using @modelcontextprotocol/inspector

.DESCRIPTION
    This script tests the .NET MCP server using the official MCP inspector tool.
    It starts the server and launches the inspector interface for interactive testing.

.PARAMETER Port
    The port to run the inspector on (default: 3000)

.PARAMETER ServerDll
    Path to the MCP server DLL (default: ./src/NugetPackagesMcpServer/bin/Release/net8.0/NugetPackagesMcpServer.dll)

.EXAMPLE
    .\test-mcp-inspector.ps1

.EXAMPLE
    .\test-mcp-inspector.ps1 -Port 3001 -ServerDll "./src/NugetPackagesMcpServer/bin/Debug/net8.0/NugetPackagesMcpServer.dll"
#>

param(
    [Parameter(Mandatory=$false)]
    [int]$Port = 3000,

    [Parameter(Mandatory=$false)]
    [string]$ServerDll = "./src/NugetPackagesMcpServer/bin/Release/net8.0/NugetPackagesMcpServer.dll"
)

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$ColorSuccess = "Green"
$ColorError = "Red"
$ColorInfo = "Cyan"
$ColorWarning = "Yellow"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Test-Prerequisites {
    Write-ColorOutput "🔍 Checking prerequisites..." $ColorInfo

    try {
        $dotnetVersion = dotnet --version
        Write-ColorOutput "✅ dotnet found: $dotnetVersion" $ColorSuccess
    }
    catch {
        Write-ColorOutput "❌ dotnet not found. Please install .NET SDK." $ColorError
        exit 1
    }

    try {
        $nodeVersion = node --version
        Write-ColorOutput "✅ Node.js found: $nodeVersion" $ColorSuccess
    }
    catch {
        Write-ColorOutput "❌ Node.js not found. Please install Node.js." $ColorError
        exit 1
    }

    try {
        $npxVersion = npx --version
        Write-ColorOutput "✅ npx found: $npxVersion" $ColorSuccess
    }
    catch {
        Write-ColorOutput "❌ npx not found. Please ensure Node.js is properly installed." $ColorError
        exit 1
    }

    
    Write-ColorOutput "🔨 Attempting to build the project (dotnet build -c release)..." $ColorInfo
    try {
        dotnet build -c release
    }
    catch {
        Write-ColorOutput "❌ Build failed: $_" $ColorError
        exit 1
    }
    if (-not (Test-Path $ServerDll)) {
        Write-ColorOutput "❌ MCP server DLL still not found after build at: $ServerDll" $ColorError
        exit 1
    }
    
    Write-ColorOutput "✅ MCP server DLL found: $ServerDll" $ColorSuccess

    Write-ColorOutput "✅ All prerequisites met!" $ColorSuccess
    Write-Host ""
}

function Install-MCPInspector {
    Write-ColorOutput "📦 Checking MCP Inspector..." $ColorInfo
    try {
        $inspectorHelp = npx @modelcontextprotocol/inspector --help 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ MCP Inspector is available" $ColorSuccess
        } else {
            Write-ColorOutput "⚠️ MCP Inspector not found, will be installed automatically by npx" $ColorWarning
        }
    }
    catch {
        Write-ColorOutput "⚠️ MCP Inspector will be installed automatically by npx" $ColorWarning
    }
    Write-Host ""
}

function Test-MCPServer {
    Write-ColorOutput "🧪 Testing MCP Server availability..." $ColorInfo
    try {
        $testResult = dotnet $ServerDll --help 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ MCP server DLL validated successfully" $ColorSuccess
        } else {
            Write-ColorOutput "❌ MCP server validation failed:" $ColorError
            Write-ColorOutput $testResult $ColorError
            exit 1
        }
    }
    catch {
        Write-ColorOutput "❌ Failed to validate MCP server: $_" $ColorError
        exit 1
    }
    Write-Host ""
}

function Start-MCPInspector {
    Write-ColorOutput "🚀 Starting MCP Inspector on port $Port..." $ColorInfo
    Write-ColorOutput "Server DLL: $ServerDll" $ColorInfo
    Write-Host ""

    Write-ColorOutput "🌐 Inspector will be available at: http://localhost:$Port" $ColorSuccess
    Write-ColorOutput "📋 The inspector will allow you to:" $ColorInfo
    Write-ColorOutput "   • View available tools and their schemas" $ColorInfo
    Write-ColorOutput "   • Test tool invocations interactively" $ColorInfo
    Write-ColorOutput "   • Inspect server responses and error handling" $ColorInfo
    Write-ColorOutput "   • Validate MCP protocol compliance" $ColorInfo
    Write-Host ""

    Write-ColorOutput "⚠️ Press Ctrl+C to stop the inspector" $ColorWarning
    Write-Host ""

    try {
        npx @modelcontextprotocol/inspector dotnet "`"$ServerDll`""
    }
    catch {
        Write-ColorOutput "❌ Failed to start MCP Inspector: $_" $ColorError
        exit 1
    }
}

function Set-EnvironmentVariables {
    Write-ColorOutput "🌱 Setting environment variable: NugetFeed:FeedUrl=https://api.nuget.org/v3/index.json" $ColorInfo
    [System.Environment]::SetEnvironmentVariable("NugetFeed:FeedUrl", "https://api.nuget.org/v3/index.json", "Process")
}

function Show-Usage {
    Write-ColorOutput "🔧 MCP Inspector Test Script" $ColorInfo
    Write-ColorOutput "This script will:" $ColorInfo
    Write-ColorOutput "1. Check prerequisites (dotnet, Node.js, MCP server DLL)" $ColorInfo
    Write-ColorOutput "2. Validate the MCP server can be loaded" $ColorInfo
    Write-ColorOutput "3. Start the MCP Inspector web interface" $ColorInfo
    Write-Host ""
}

try {
    Write-Host ""
    Write-ColorOutput "🧪 Nuget Packages MCP Server - MCP Inspector Test" $ColorInfo
    Write-ColorOutput ("=" * 60) $ColorInfo
    Write-Host ""

    Show-Usage
    Test-Prerequisites
    Install-MCPInspector
    Test-MCPServer
    Set-EnvironmentVariables
    Start-MCPInspector
}
catch {
    Write-ColorOutput "❌ Script failed: $_" $ColorError
    exit 1
}
finally {
    Write-Host ""
    Write-ColorOutput "🏁 Test script completed" $ColorInfo
}

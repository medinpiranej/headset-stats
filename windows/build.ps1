<#
.SYNOPSIS
    Builds, tests and packages Headset Stats for Windows.

.DESCRIPTION
    1. Restores and builds the solution (warnings are errors).
    2. Runs the unit tests (skip with -SkipTests).
    3. Publishes the tray app (HeadsetStats.exe) and the probe tool (headset-probe.exe)
       into artifacts\HeadsetStats-<version>-<runtime>\ and zips that folder.

    Works in Windows PowerShell 5.1 and PowerShell 7. Requires the .NET 9 SDK.

.PARAMETER Configuration
    Release (default) or Debug.

.PARAMETER Runtime
    win-x64 (default) or win-arm64.

.PARAMETER SelfContained
    Bundle the .NET runtime so the app runs on PCs without .NET installed (much larger download).
    Without it, the PC needs the .NET 9 Desktop Runtime: winget install Microsoft.DotNet.DesktopRuntime.9

.PARAMETER SkipTests
    Don't run the unit tests.

.PARAMETER Output
    Where to put the published folder and zip (default: <repo>\artifacts).

.EXAMPLE
    .\build.ps1
.EXAMPLE
    .\build.ps1 -SelfContained -Runtime win-arm64
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')] [string] $Configuration = 'Release',
    [ValidateSet('win-x64', 'win-arm64')] [string] $Runtime = 'win-x64',
    [switch] $SelfContained,
    [switch] $SkipTests,
    [string] $Output = ''
)

$ErrorActionPreference = 'Stop'
# $PSScriptRoot is empty in param() defaults on Windows PowerShell 5.1, so resolve the default here.
if (-not $Output) { $Output = Join-Path $PSScriptRoot '..\artifacts' }
Set-Location $PSScriptRoot

function Invoke-Step([string] $Title, [scriptblock] $Command) {
    Write-Host "==> $Title" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Title failed (exit code $LASTEXITCODE)." }
}

# A freshly installed SDK isn't on PATH until a new terminal is opened; pick it up anyway.
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK was not found. Install it with: winget install Microsoft.DotNet.SDK.9'
}
$sdks = & dotnet --list-sdks
if (-not ($sdks | Where-Object { $_ -match '^9\.' })) {
    throw "The .NET 9 SDK is required (found: $($sdks -join ', ')). Install it with: winget install Microsoft.DotNet.SDK.9"
}

# A running app locks its build output.
if (Get-Process HeadsetStats -ErrorAction SilentlyContinue) {
    Write-Warning 'Headset Stats is running; close it (tray icon > Exit) if the build fails with "file is locked".'
}

$version = ([xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props'))).Project.PropertyGroup.Version
$suffix = ''
if ($SelfContained) { $suffix = '-selfcontained' }
$name = "HeadsetStats-$version-$Runtime$suffix"
$publishDir = Join-Path $Output $name
$selfContainedValue = 'false'
if ($SelfContained) { $selfContainedValue = 'true' }

Invoke-Step 'Restore' { dotnet restore HeadsetStats.sln }
Invoke-Step "Build ($Configuration)" { dotnet build HeadsetStats.sln -c $Configuration --no-restore }
if (-not $SkipTests) {
    Invoke-Step 'Test' { dotnet test HeadsetStats.sln -c $Configuration --no-build }
}

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
foreach ($project in 'src\HeadsetStats.Tray', 'src\HeadsetStats.Probe') {
    Invoke-Step "Publish $project ($Runtime)" {
        dotnet publish $project -c $Configuration -r $Runtime --self-contained $selfContainedValue `
            -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none `
            -o $publishDir
    }
}
Copy-Item (Join-Path $PSScriptRoot '..\LICENSE') $publishDir
Copy-Item (Join-Path $PSScriptRoot '..\PRIVACY.md') $publishDir

$zip = "$publishDir.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zip

$folder = (Resolve-Path $publishDir).Path
Write-Host ''
Write-Host "Done. Headset Stats $version ($Runtime)" -ForegroundColor Green
Write-Host "  Folder: $folder"
Write-Host "  Zip:    $((Resolve-Path $zip).Path)"
Write-Host "  Run:    $folder\HeadsetStats.exe"
if (-not $SelfContained) {
    Write-Host '  Needs the .NET 9 Desktop Runtime on the PC (winget install Microsoft.DotNet.DesktopRuntime.9), or build with -SelfContained.'
}

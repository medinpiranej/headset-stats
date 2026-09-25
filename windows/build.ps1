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

.PARAMETER Msix
    Also build the Microsoft Store package (artifacts\HeadsetStats-<version>-<runtime>.msix): a self-contained
    publish of the tray app plus packaging\AppxManifest.xml, with the identity from packaging\identity.json.
    Needs the Windows SDK (winget install Microsoft.WindowsSDK.10.0.26100). Upload the .msix to Partner Center
    unsigned; the Store signs it.

.EXAMPLE
    .\build.ps1
.EXAMPLE
    .\build.ps1 -SelfContained -Runtime win-arm64
.EXAMPLE
    .\build.ps1 -Msix
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')] [string] $Configuration = 'Release',
    [ValidateSet('win-x64', 'win-arm64')] [string] $Runtime = 'win-x64',
    [switch] $SelfContained,
    [switch] $SkipTests,
    [string] $Output = '',
    [switch] $Msix
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

if ($Msix) {
    $makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if (-not $makeappx) { throw 'makeappx.exe not found. Install the Windows SDK: winget install Microsoft.WindowsSDK.10.0.26100' }

    # Store apps can't rely on a separately installed .NET runtime, so the package is self-contained.
    $layout = Join-Path $Output "msix-layout-$Runtime"
    if (Test-Path $layout) { Remove-Item $layout -Recurse -Force }
    Invoke-Step "Publish tray app for MSIX ($Runtime, self-contained)" {
        dotnet publish src\HeadsetStats.Tray -c $Configuration -r $Runtime --self-contained true -p:DebugType=none -o $layout
    }
    Copy-Item (Join-Path $PSScriptRoot 'packaging\Assets') (Join-Path $layout 'Assets') -Recurse

    $identity = Get-Content (Join-Path $PSScriptRoot 'packaging\identity.json') -Raw | ConvertFrom-Json
    $architecture = $Runtime.Substring(4)  # win-x64 -> x64, win-arm64 -> arm64
    $manifest = Get-Content (Join-Path $PSScriptRoot 'packaging\AppxManifest.xml') -Raw
    $tokens = [ordered]@{
        '$PublisherDisplayName$' = $identity.PublisherDisplayName  # before $Publisher$, which it contains
        '$Publisher$'            = $identity.Publisher
        '$Name$'                 = $identity.Name
        '$Version$'              = "$version.0"
        '$Architecture$'         = $architecture
    }
    foreach ($token in $tokens.Keys) { $manifest = $manifest.Replace($token, $tokens[$token]) }
    [IO.File]::WriteAllText((Join-Path $layout 'AppxManifest.xml'), $manifest, (New-Object Text.UTF8Encoding $false))

    $msixPath = Join-Path $Output "HeadsetStats-$version-$Runtime.msix"
    Invoke-Step 'Pack MSIX' { & $makeappx.FullName pack /d $layout /p $msixPath /o }
    if ($identity.Name -like '*LocalTest*') {
        Write-Warning 'packaging\identity.json still has the local-test identity; put in the Partner Center values before uploading.'
    }
}

$folder = (Resolve-Path $publishDir).Path
Write-Host ''
Write-Host "Done. Headset Stats $version ($Runtime)" -ForegroundColor Green
Write-Host "  Folder: $folder"
Write-Host "  Zip:    $((Resolve-Path $zip).Path)"
Write-Host "  Run:    $folder\HeadsetStats.exe"
if (-not $SelfContained) {
    Write-Host '  Needs the .NET 9 Desktop Runtime on the PC (winget install Microsoft.DotNet.DesktopRuntime.9), or build with -SelfContained.'
}
if ($Msix) {
    Write-Host "  MSIX:   $((Resolve-Path $msixPath).Path)"
}


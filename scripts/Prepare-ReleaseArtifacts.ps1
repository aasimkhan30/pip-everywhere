[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string]$Version,
    [ValidatePattern('^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$')]
    [string]$Repository = 'aasimkhan30/pip-everywhere'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $repositoryRoot 'artifacts\msix'
$releaseRoot = Join-Path $outputRoot 'release'
$publisher = 'CN=Aasim Khan'
$releaseVersion = $Version.Substring(0, $Version.LastIndexOf('.'))
$tag = "v$releaseVersion"
$releaseBaseUri = "https://github.com/$Repository/releases/download/$tag"
$feedBaseUri = "https://github.com/$Repository/releases/latest/download"

if (Test-Path -LiteralPath $releaseRoot) {
    $resolvedOutputRoot = [System.IO.Path]::GetFullPath($outputRoot)
    $resolvedReleaseRoot = [System.IO.Path]::GetFullPath($releaseRoot)
    if (-not $resolvedReleaseRoot.StartsWith(
        $resolvedOutputRoot + [System.IO.Path]::DirectorySeparatorChar,
        [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove release output outside $resolvedOutputRoot."
    }

    Remove-Item -LiteralPath $resolvedReleaseRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $releaseRoot | Out-Null

foreach ($architecture in @('x64', 'arm64')) {
    $installerFolder = Join-Path $outputRoot "PiPEverywhere-$architecture"
    if (-not (Test-Path -LiteralPath $installerFolder)) {
        throw "Installer folder was not found: $installerFolder"
    }

    $sourcePackage = Get-ChildItem -LiteralPath $installerFolder -Filter '*.msix' |
        Where-Object Name -NotLike '*.appxsym' |
        Select-Object -First 1
    if (-not $sourcePackage) {
        throw "The $architecture application package was not found."
    }

    $sourceDependency = Join-Path $installerFolder "Dependencies\$architecture\Microsoft.WindowsAppRuntime.2.msix"
    if (-not (Test-Path -LiteralPath $sourceDependency)) {
        throw "The $architecture Windows App Runtime dependency was not found."
    }

    $packageName = "PiPEverywhere-$architecture.msix"
    $dependencyName = "Microsoft.WindowsAppRuntime.2-$architecture.msix"
    $appInstallerName = "PiPEverywhere-$architecture.appinstaller"

    Copy-Item -LiteralPath $sourcePackage.FullName -Destination (Join-Path $releaseRoot $packageName)
    Copy-Item -LiteralPath $sourceDependency -Destination (Join-Path $releaseRoot $dependencyName)

    $appInstaller = @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller
  xmlns="http://schemas.microsoft.com/appx/appinstaller/2018"
  Uri="$feedBaseUri/$appInstallerName"
  Version="$Version">
  <MainPackage
    Name="PiPEverywhere"
    Publisher="$publisher"
    Version="$Version"
    ProcessorArchitecture="$architecture"
    Uri="$releaseBaseUri/$packageName" />
  <Dependencies>
    <Package
      Name="Microsoft.WindowsAppRuntime.2"
      Publisher="CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US"
      Version="2.3.1.0"
      ProcessorArchitecture="$architecture"
      Uri="$releaseBaseUri/$dependencyName" />
  </Dependencies>
  <UpdateSettings>
    <OnLaunch HoursBetweenUpdateChecks="0" />
    <AutomaticBackgroundTask />
  </UpdateSettings>
</AppInstaller>
"@

    $appInstallerPath = Join-Path $releaseRoot $appInstallerName
    [System.IO.File]::WriteAllText(
        $appInstallerPath,
        $appInstaller,
        [System.Text.UTF8Encoding]::new($false)
    )

    Copy-Item -LiteralPath $appInstallerPath -Destination $installerFolder -Force

    $archivePath = Join-Path $outputRoot "PiPEverywhere-$architecture-$releaseVersion.zip"
    Compress-Archive -Path (Join-Path $installerFolder '*') -DestinationPath $archivePath -Force
}

$certificate = Join-Path $outputRoot 'PiPEverywhere-x64\PiPEverywhere.cer'
if (-not (Test-Path -LiteralPath $certificate)) {
    throw 'The public signing certificate was not found.'
}
Copy-Item -LiteralPath $certificate -Destination (Join-Path $releaseRoot 'PiPEverywhere.cer')

Write-Host "Release artifacts prepared in $releaseRoot" -ForegroundColor Green

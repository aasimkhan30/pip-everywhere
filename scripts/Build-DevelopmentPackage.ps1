[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [ValidateSet('x64', 'arm64')]
    [string]$Architecture = 'x64',
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string]$Version = '0.0.1.0'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'PiPEverywhere.csproj'
$manifestPath = Join-Path $repositoryRoot 'Package.appxmanifest'
$outputPath = Join-Path $repositoryRoot 'artifacts\msix'
$publisher = 'CN=Aasim Khan'
$platform = if ($Architecture -eq 'arm64') { 'ARM64' } else { 'x64' }
$runtimeIdentifier = "win-$Architecture"

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) {
    throw 'Visual Studio Installer (vswhere.exe) was not found.'
}

$visualStudioPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if (-not $visualStudioPath) {
    throw 'A Visual Studio installation with MSBuild was not found.'
}

$msbuild = Join-Path $visualStudioPath 'MSBuild\Current\Bin\MSBuild.exe'
$signTool = Get-ChildItem -Path (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin') `
    -Filter signtool.exe -Recurse |
    Where-Object FullName -Match '\\x64\\signtool\.exe$' |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $signTool) {
    throw 'SignTool.exe was not found. Install the Windows SDK signing tools.'
}

$originalManifestBytes = [System.IO.File]::ReadAllBytes($manifestPath)
$originalManifest = [System.Text.Encoding]::UTF8.GetString($originalManifestBytes).TrimStart([char]0xFEFF)
$versionedManifest = [regex]::Replace(
    $originalManifest,
    '(<Identity\b[\s\S]*?\bVersion=")[^"]+(")',
    { param($match) $match.Groups[1].Value + $Version + $match.Groups[2].Value },
    1
)

try {
    [System.IO.File]::WriteAllText(
        $manifestPath,
        $versionedManifest,
        [System.Text.UTF8Encoding]::new($true)
    )

    & $msbuild $projectPath `
        /restore `
        "/p:Configuration=$Configuration" `
        "/p:Platform=$platform" `
        "/p:RuntimeIdentifier=$runtimeIdentifier" `
        /p:GenerateAppxPackageOnBuild=true `
        /p:AppxBundle=Never `
        /p:AppxPackageSigningEnabled=false `
        "/p:AppxPackageDir=$outputPath\" `
        /verbosity:minimal

    if ($LASTEXITCODE -ne 0) {
        throw "MSIX build failed with exit code $LASTEXITCODE."
    }
}
finally {
    [System.IO.File]::WriteAllBytes($manifestPath, $originalManifestBytes)
}

$certificate = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object {
        $_.Subject -eq $publisher -and
        $_.HasPrivateKey -and
        $_.EnhancedKeyUsageList.ObjectId -contains '1.3.6.1.5.5.7.3.3' -and
        $_.NotAfter -gt (Get-Date).AddMonths(6)
    } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (-not $certificate) {
    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $publisher `
        -FriendlyName 'PiP Everywhere Development Signing' `
        -CertStoreLocation Cert:\CurrentUser\My `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears(2)
}

$packageFolder = Get-ChildItem -LiteralPath $outputPath -Directory |
    Where-Object Name -Like "PiPEverywhere_*_${Architecture}_Test" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $packageFolder) {
    throw 'The generated MSIX output folder was not found.'
}

$package = Get-ChildItem -LiteralPath $packageFolder.FullName -Filter '*.msix' |
    Where-Object Name -NotLike '*.appxsym' |
    Select-Object -First 1

if (-not $package) {
    throw 'The generated MSIX package was not found.'
}

$publicCertificate = Join-Path $packageFolder.FullName 'PiPEverywhere-development.cer'
Export-Certificate -Cert $certificate -FilePath $publicCertificate -Force | Out-Null

& $signTool sign /fd SHA256 /sha1 $certificate.Thumbprint $package.FullName
if ($LASTEXITCODE -ne 0) {
    throw "Package signing failed with exit code $LASTEXITCODE."
}

Copy-Item `
    -LiteralPath (Join-Path $PSScriptRoot 'Install-PiPEverywhere.ps1') `
    -Destination (Join-Path $packageFolder.FullName 'Install-PiPEverywhere.ps1') `
    -Force

$releaseFolder = Join-Path $outputPath "PiPEverywhere-$Architecture"
if (Test-Path -LiteralPath $releaseFolder) {
    Remove-Item -LiteralPath $releaseFolder -Recurse -Force
}

New-Item -ItemType Directory -Path $releaseFolder | Out-Null
Copy-Item -LiteralPath $package.FullName -Destination $releaseFolder
Copy-Item -LiteralPath $publicCertificate -Destination $releaseFolder
Copy-Item `
    -LiteralPath (Join-Path $PSScriptRoot 'Install-PiPEverywhere.ps1') `
    -Destination $releaseFolder
Copy-Item `
    -LiteralPath (Join-Path $repositoryRoot 'README.md') `
    -Destination $releaseFolder

$dependencySource = Join-Path $packageFolder.FullName 'Dependencies'
if (Test-Path -LiteralPath $dependencySource) {
    Copy-Item `
        -LiteralPath $dependencySource `
        -Destination (Join-Path $releaseFolder 'Dependencies') `
        -Recurse
}

$releaseArchive = Join-Path $outputPath "PiPEverywhere-$Architecture-$($Version.Substring(0, $Version.LastIndexOf('.'))).zip"
Compress-Archive -Path (Join-Path $releaseFolder '*') -DestinationPath $releaseArchive -Force

Write-Host ''
Write-Host 'Development package created:' -ForegroundColor Green
Write-Host $packageFolder.FullName
Write-Host ''
Write-Host 'Shareable installer archive:' -ForegroundColor Green
Write-Host $releaseArchive
Write-Host ''
Write-Host 'Run Install-PiPEverywhere.ps1 from that folder to install it.'

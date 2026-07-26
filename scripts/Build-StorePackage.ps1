[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [ValidatePattern('^\d+\.\d+\.\d+\.0$')]
    [string]$Version = '0.0.5.0'
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'PiPEverywhere.csproj'
$manifestPath = Join-Path $repositoryRoot 'Package.appxmanifest'
$artifactsRoot = Join-Path $repositoryRoot 'artifacts'
$outputPath = Join-Path $artifactsRoot 'store'
$publisher = 'CN=8D93E30E-E8CA-4DBD-9FAA-C280229BB5D5'

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) {
    throw 'Visual Studio Installer (vswhere.exe) was not found.'
}

$visualStudioPath = & $vswhere `
    -latest `
    -products * `
    -requires Microsoft.Component.MSBuild `
    -property installationPath
if (-not $visualStudioPath) {
    throw 'A Visual Studio installation with MSBuild was not found.'
}

$msbuild = Join-Path $visualStudioPath 'MSBuild\Current\Bin\MSBuild.exe'

$certificate = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object {
        $_.Subject -eq $publisher -and
        $_.HasPrivateKey -and
        $_.EnhancedKeyUsageList.ObjectId -contains '1.3.6.1.5.5.7.3.3' -and
        $_.NotAfter -gt (Get-Date).AddMonths(1)
    } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (-not $certificate) {
    # This certificate is used only to create a valid Partner Center upload.
    # Microsoft re-signs the accepted package for Store distribution.
    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $publisher `
        -FriendlyName 'PiP Everywhere Store Upload Signing' `
        -CertStoreLocation Cert:\CurrentUser\My `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears(1)
}

if (Test-Path -LiteralPath $outputPath) {
    $resolvedArtifactsRoot = [System.IO.Path]::GetFullPath($artifactsRoot)
    $resolvedOutputPath = [System.IO.Path]::GetFullPath($outputPath)
    if (-not $resolvedOutputPath.StartsWith(
        $resolvedArtifactsRoot + [System.IO.Path]::DirectorySeparatorChar,
        [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove Store output outside $resolvedArtifactsRoot."
    }

    Remove-Item -LiteralPath $resolvedOutputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $outputPath | Out-Null

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
        /p:Platform=x64 `
        /p:GenerateAppxPackageOnBuild=true `
        /p:AppxBundle=Always `
        '/p:AppxBundlePlatforms=x64|ARM64' `
        /p:UapAppxPackageBuildMode=StoreUpload `
        /p:AppxPackageSigningEnabled=true `
        "/p:PackageCertificateThumbprint=$($certificate.Thumbprint)" `
        "/p:AppxPackageDir=$outputPath\" `
        /verbosity:minimal

    if ($LASTEXITCODE -ne 0) {
        throw "Store package build failed with exit code $LASTEXITCODE."
    }
}
finally {
    [System.IO.File]::WriteAllBytes($manifestPath, $originalManifestBytes)
}

$uploadPackage = Get-ChildItem -LiteralPath $outputPath -Filter '*.msixupload' |
    Select-Object -First 1
if (-not $uploadPackage) {
    throw 'The Store .msixupload package was not generated.'
}

$bundle = Get-ChildItem -LiteralPath $outputPath -Filter '*.msixbundle' -Recurse |
    Select-Object -First 1
if (-not $bundle) {
    throw 'The x64/ARM64 MSIX bundle was not generated.'
}

$signature = Get-AuthenticodeSignature -LiteralPath $bundle.FullName
if (-not $signature.SignerCertificate -or
    $signature.SignerCertificate.Thumbprint -ne $certificate.Thumbprint) {
    throw 'The Store bundle signature does not match the upload certificate.'
}

Write-Host ''
Write-Host 'Microsoft Store upload package created:' -ForegroundColor Green
Write-Host $uploadPackage.FullName
Write-Host ''
Write-Host 'Architectures: x64, ARM64'
Write-Host "Version: $Version"


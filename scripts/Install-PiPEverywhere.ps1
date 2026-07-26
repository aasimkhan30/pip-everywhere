[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$principal = [Security.Principal.WindowsPrincipal]::new(
    [Security.Principal.WindowsIdentity]::GetCurrent())
$isAdministrator = $principal.IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdministrator) {
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    Start-Process powershell.exe -Verb RunAs -ArgumentList $arguments
    return
}

$packageFolder = $PSScriptRoot
$certificate = Join-Path $packageFolder 'PiPEverywhere.cer'
$appInstaller = Get-ChildItem -LiteralPath $packageFolder -Filter '*.appinstaller' |
    Select-Object -First 1
$package = Get-ChildItem -LiteralPath $packageFolder -Filter '*.msix' |
    Where-Object Name -NotLike '*.appxsym' |
    Select-Object -First 1

if (-not (Test-Path -LiteralPath $certificate)) {
    throw 'The PiP Everywhere signing certificate was not found beside this script.'
}

if (-not $appInstaller -and -not $package) {
    throw 'The PiP Everywhere MSIX package was not found beside this script.'
}

& certutil.exe -addstore TrustedPeople $certificate
if ($LASTEXITCODE -ne 0) {
    throw "Certificate installation failed with exit code $LASTEXITCODE."
}

if ($appInstaller) {
    Add-AppxPackage -AppInstallerFile $appInstaller.FullName
}
else {
    $dependencyPaths = @(
        Get-ChildItem `
            -LiteralPath (Join-Path $packageFolder 'Dependencies') `
            -Filter '*.msix' `
            -Recurse `
            -ErrorAction SilentlyContinue
    ) | Select-Object -ExpandProperty FullName

    $installParameters = @{
        Path = $package.FullName
        ForceApplicationShutdown = $true
    }

    if ($dependencyPaths.Count -gt 0) {
        $installParameters.DependencyPath = $dependencyPaths
    }

    Add-AppxPackage @installParameters
}

$startEntry = Get-StartApps | Where-Object Name -eq 'PiP Everywhere' | Select-Object -First 1
if ($startEntry) {
    Start-Process "shell:AppsFolder\$($startEntry.AppID)"
}

Write-Host 'PiP Everywhere was installed successfully.' -ForegroundColor Green

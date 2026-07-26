# PiP Everywhere

PiP Everywhere is a small WinUI 3 utility that keeps browser
picture-in-picture windows visible across every Windows virtual desktop
without pinning the browser's normal windows.

## Features

- Select Microsoft Edge, Google Chrome, Mozilla Firefox, Brave, Opera, or
  Vivaldi independently.
- Automatically pin new PiP windows and unpin app-owned windows when a
  browser is deselected.
- Start at sign-in through the Windows packaged startup-task API.
- Continue quietly in the notification area when the window is closed.
- Store settings locally; no browser extension or browsing permissions.
- Package as x64 and ARM64 MSIX installers.

## Requirements

- Windows 11 24H2 or newer, x64 or ARM64
- .NET 10 SDK and the Windows App SDK workload to build from source

The virtual-desktop pinning API used by Windows is not public. PiP Everywhere
bundles the MIT-licensed VirtualDesktop 1.21 helper and may need compatibility
updates after major Windows releases.

## Build and test

```powershell
dotnet restore .\PiPEverywhere.slnx
dotnet build .\PiPEverywhere.slnx -c Release
dotnet test .\tests\PiPEverywhere.Tests\PiPEverywhere.Tests.csproj -c Release
```

## Build an installable development package

Run:

```powershell
.\scripts\Build-DevelopmentPackage.ps1 -Architecture x64
.\scripts\Build-DevelopmentPackage.ps1 -Architecture arm64
```

The script creates and signs an MSIX with a free self-signed development
certificate. Open the resulting folder under `artifacts\msix`, right-click
`Install-PiPEverywhere.ps1`, and choose **Run with PowerShell**. Windows asks
for administrator approval once so it can trust that development certificate.

Development certificates are suitable for personal testing. For public
distribution, use the Microsoft Store (Microsoft signs MSIX submissions) or
replace the development certificate with a trusted code-signing service.

## Releases

Every successful build from `main` creates a GitHub release with x64 and ARM64
installer archives. Releases begin at `0.0.1` and automatically increment the
patch number (`0.0.2`, `0.0.3`, and so on).

## Privacy

The watcher enumerates top-level windows and compares only:

- process name;
- window class;
- the standard PiP window title.

It does not read tabs, URLs, history, cookies, page content, or video content.

## License

PiP Everywhere is MIT licensed. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)
for bundled dependencies.

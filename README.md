# PiP Everywhere

PiP Everywhere is a small WinUI 3 utility that keeps browser
picture-in-picture windows visible across every Windows virtual desktop
without pinning the browser's normal windows.

## Features

- Select Microsoft Edge, Google Chrome, Mozilla Firefox, Brave, Opera, or
  Vivaldi independently.
- Pause and resume PiP watching from a persistent quick switch.
- Filter the browser list and see which browsers are detected locally.
- Automatically pin new PiP windows and unpin app-owned windows when a
  browser is deselected.
- Start at sign-in through the Windows packaged startup-task API.
- Continue quietly in the notification area when the window is closed, with
  a right-click **Quit** action.
- Store settings locally; no browser extension or browsing permissions.
- Publish one Microsoft Store package supporting x64 and ARM64.

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

Development certificates are suitable only for local testing. Public
installation, signing, and updates are handled by the Microsoft Store.

## Microsoft Store releases

PiP Everywhere's Store product ID is `9N9S061H2Z1S`. The Store identity is:

- Package name: `AasimKhan.PiPEverywhere`
- Publisher: `CN=8D93E30E-E8CA-4DBD-9FAA-C280229BB5D5`
- Publisher display name: `Aasim Khan`

Build the multi-architecture Store upload locally with:

```powershell
.\scripts\Build-StorePackage.ps1 -Version 0.0.5.0
```

Every push to `main` tests the app, assigns a new increasing patch version,
builds one `.msixupload` containing x64 and ARM64, and retains it as a workflow
artifact. The first Store submission must be completed in Partner Center.
After that version is live and the repository variable
`STORE_PUBLISHING_ENABLED` is `true`, the same workflow submits validated
updates automatically and tags each accepted commit. Submission requires the
Partner Center credentials documented in the workflow to be stored as GitHub
environment secrets.

## Privacy

The watcher enumerates top-level windows and compares only:

- process name;
- window class;
- the standard PiP window title.

It does not read tabs, URLs, history, cookies, page content, or video content.

## License

PiP Everywhere is MIT licensed. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)
for bundled dependencies.

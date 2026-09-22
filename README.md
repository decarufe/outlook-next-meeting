# OutlookNextEvent

OutlookNextEvent is a Windows 11 widget that displays the next Outlook calendar events in the Widgets board. The MVP is a packaged WinUI 3 / Windows App SDK app with a companion sign-in window, a widget provider, pure core logic, and infrastructure adapters for Microsoft Graph and MSAL/WAM.

## Project layout

- `src\OutlookNextEvent.Core` — pure .NET 8 calendar/auth contracts and event shaping logic.
- `src\OutlookNextEvent.Infrastructure` — .NET 8 Windows infrastructure stubs for Microsoft Graph, MSAL/WAM, and settings.
- `src\OutlookNextEvent.App` — packaged WinUI 3 app, companion `MainWindow`, widget provider manifest registration, and Adaptive Card placeholders.
- `tests\OutlookNextEvent.Core.Tests` — xUnit tests for core logic.
- `tests\OutlookNextEvent.App.Tests` — xUnit scaffold for app/infrastructure-facing tests.

## Prerequisites

- Windows 11 23H2 or newer recommended for third-party widget validation.
- Visual Studio 2022 with .NET desktop development, Windows application development, Windows App SDK tooling, and MSIX packaging support.
- .NET SDK 8.0 or newer. `global.json` pins the minimum to 8.0 and allows roll-forward to newer installed SDKs.
- Developer Mode enabled for local MSIX sideloading.
- An Entra ID public client app registration for later auth work:
  - Mobile and desktop redirect URI: `ms-appx-web://Microsoft.AAD.BrokerPlugin/{ApplicationClientId}`
  - Public client flows enabled
  - Microsoft Graph delegated permission: `Calendars.Read`
  - Configure the app's `AuthSettings.ClientId` with the Application (client) ID; no client secret is used or stored.

The broker redirect URI is configurable through `AuthSettings.RedirectUri`. The default follows the current MSAL/WAM guidance above, but the exact packaged redirect URI must be confirmed in the Entra registration for the final MSIX identity. Token cache persistence is still a follow-up; this implementation uses MSAL's public client cache for the current process and exposes sign-out to remove cached accounts.

## Build

```powershell
dotnet restore .\OutlookNextEvent.sln
dotnet build .\OutlookNextEvent.sln
```

In headless environments without Windows App SDK/MSIX tooling, build the pure core path first:

```powershell
dotnet build .\src\OutlookNextEvent.Core\OutlookNextEvent.Core.csproj
dotnet test .\tests\OutlookNextEvent.Core.Tests\OutlookNextEvent.Core.Tests.csproj
```

## Installation / Sideload

Prerequisites:

- Windows 11 with third-party widget support.
- Developer Mode enabled.
- Windows App SDK/MSIX tooling installed for package generation.

Build the signed local MSIX package from the repository root:

```powershell
.\build-package.ps1
```

This creates root-level sideload artifacts such as `OutlookNextEvent_0.1.0.0_x64.msix` and `OutlookNextEventDev.cer`. To install them, open PowerShell **as Administrator** and run the one-shot helper:

```powershell
.\install-package.ps1
```

Manual equivalent, also from an elevated PowerShell:

```powershell
Import-Certificate -FilePath .\OutlookNextEventDev.cer -CertStoreLocation Cert:\LocalMachine\Root
Import-Certificate -FilePath .\OutlookNextEventDev.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
Add-AppxPackage -Path .\OutlookNextEvent_0.1.0.0_x64.msix
Get-AppxPackage -Name Decarufe.OutlookNextEvent
```

If `Add-AppxPackage` fails with `0x800B010A` / `CERT_E_UNTRUSTEDROOT`, the self-signed development root is not trusted. Re-run the `LocalMachine\Root` import from an elevated PowerShell, or use `.\install-package.ps1`.

See `docs\packaging.md` for full packaging, trust, verification, and loose Developer Mode registration details.

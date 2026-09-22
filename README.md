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

## Sideload

Use Visual Studio on Windows 11 with the Windows App SDK/MSIX tooling installed to package and deploy `src\OutlookNextEvent.App`. The widget manifest registers a single `NextEventsWidget` provider via `windows.comServer` and `windows.appExtension` using COM `CreateInstance`.

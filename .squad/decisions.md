# Squad Decisions

## Active Decisions

### 2026-09-22T10:37:31-04:00: Proposition d’architecture MVP pour OutlookNextEvent
**By:** Holden
**What:** Choisir une application C#/.NET packagée MSIX avec Windows App SDK pour implémenter un provider `IWidgetProvider`, rendu via Adaptive Cards, données calendrier via Microsoft Graph `/me/calendarView`, et authentification MSAL/Entra ID en public client desktop.
**Why:** Cette pile correspond directement aux contraintes des widgets tiers Windows 11, garde le MVP petit, et sépare clairement provider widget, rendu carte, auth et accès calendrier.

**Packaging:** MSIX d’abord en sideload/dev package, identité applicative stable, manifeste déclarant un seul widget `NextEventsWidget`; Store/signature/pipeline production différés jusqu’à validation du provider et de l’auth.

**Top 3 questions ouvertes:**
1. Version minimale exacte Windows 11 / Windows App SDK et contraintes de disponibilité production des widgets tiers.
2. Schéma exact du manifeste widget/MSIX et activation provider selon la version Windows App SDK retenue.
3. Configuration MSAL pour MSIX : broker WAM, redirect URI et comportement d’un login lancé depuis une action de widget.

### 2026-09-22T10:56:53-04:00: MVP v1 GitHub issue breakdown
**By:** Holden
**What:** Created 15 GitHub issues in `decarufe/outlook-next-meeting` for MVP v1 scope: #1 epic MVP; #2-#4 spikes; #5-#11 implementation; #12-#14 test strategy/tests; #15 post-v1 backlog epic. Labels include `squad:holden`, `squad:naomi`, `squad:alex`, and `squad:amos`.
**Why:** The issue set converts the MVP architecture proposal into routed, reviewable work items for Holden, Naomi, Alex, and Amos.

### 2026-09-22T12:40:46-04:00: Windows widget platform and manifest contract
**By:** Holden
**What:** OutlookNextEvent targets a packaged Win32/C# MSIX app using the current stable Windows App SDK 2.x line, with Windows 11 23H2/24H2 validation before promising production support. The package exposes one `NextEventsWidget` (`AllowMultiple=false`, small/medium/large) through `windows.comServer` plus `windows.appExtension` named `com.microsoft.windows.widgets`; provider activation uses a stable CLSID and sends Adaptive Card content through `WidgetManager.UpdateWidget` during create/activate rather than embedding the template in `Package.appxmanifest`.
**Why:** This follows the supported third-party widget provider model while keeping MVP compatibility claims tied to tested Windows/SDK versions.

### 2026-09-22T12:40:46-04:00: Auth and Graph integration boundaries
**By:** Alex
**What:** The MVP uses MSAL.NET public-client authentication with WAM broker enabled in a packaged companion window that supplies an HWND to `WithParentActivityOrWindow`. Entra configuration uses the broker redirect `ms-appx-web://Microsoft.AAD.BrokerPlugin/{ApplicationClientId}`, public client flows, and delegated Graph scopes `Calendars.Read` plus `User.Read` only if account UI requires it. `MsalAuthService` owns token acquisition/sign-out, while `GraphCalendarClient` only fetches `/me/calendarView` and maps raw Graph fields to `NextEvent`; sorting, filtering, timezone conversion, and presentation shaping remain outside the adapters.
**Why:** WAM requires interactive UI parented to a window, and keeping adapters thin preserves testable seams for EventShaper, cards, and provider lifecycle work.

### 2026-09-22T12:40:46-04:00: Widget provider lifecycle boundary
**By:** Holden
**What:** `NextEventsWidgetProvider` owns widget instance tracking, COM activation, refresh/connect actions, and placeholder updates through `IWidgetHost`, `INextEventsWidgetContentProvider`, and `ICompanionSignInLauncher`. Event shaping and final Adaptive Card rendering remain delegated to `EventShaper` and `CardBuilder`.
**Why:** The provider stays focused on the Windows Widgets contract and avoids taking responsibility for Graph/auth, sorting, timezone shaping, or direct interactive MSAL callbacks.

### 2026-09-22T12:40:46-04:00: EventShaper owns next-event shaping semantics
**By:** Alex, Amos
**What:** `EventShaper` converts raw `NextEvent` calendar items into `NextEventsViewModel`, applying cancellation filtering, upcoming-window filtering, target-time-zone conversion, all-day date shaping, ordering, and max-count capping before card rendering. Tests lock the boundary semantics: events ending exactly at `now` are excluded; events starting exactly at `now` and already-started events with future end times are included; `windowEnd` is inclusive for event start time.
**Why:** A stable shaped model keeps Graph/MSAL adapters and card rendering separate and gives card/provider tests deterministic behavior through explicit `now` values and `TimeProvider` seams.

### 2026-09-22T12:40:46-04:00: CardBuilder v1 contract and tests
**By:** Naomi, Amos
**What:** `CardBuilder` lives in `OutlookNextEvent.Core.Cards` and consumes `NextEventsViewModel` to emit Adaptive Card JSON plus empty data JSON for loading, signed-out, sign-in-requested, next-events, empty, and error/retry states. The template file remains the v1 structural reference, while tests assert schema essentials, action verbs/titles, recursive text content, and one EventShaper-to-CardBuilder path using structured JSON rather than whole-string snapshots.
**Why:** This keeps final card rendering unit-testable without loading the Windows App SDK app assembly and catches contract regressions without brittle JSON ordering snapshots.

### 2026-09-22T12:40:46-04:00: MSIX dev and sideload packaging
**By:** Naomi
**What:** The MVP keeps single-project MSIX packaging in `src\OutlookNextEvent.App` instead of adding a separate `.wapproj`. The app project owns the manifest, asset inclusion, sideload build mode, x64 package settings, and conditional signing via local `PackageCertificateKeyFile`. The manifest uses package identity `Decarufe.OutlookNextEvent` / `CN=OutlookNextEventDev`, declares `internetClient` and `runFullTrust`, and registers `NextEventsWidget` with CLSID `8F3D7C7B-8D0D-41BF-8D9E-608A70D03E95`.
**Why:** Single-project packaging is sufficient for the v1 sideload/dev loop, while keeping signing material local-only and documenting certificate/install/uninstall verification in `docs\packaging.md`.

### 2026-09-22T12:40:46-04:00: Offline deterministic MVP test strategy
**By:** Amos
**What:** Automated MVP tests remain offline and deterministic with reusable support in `tests\OutlookNextEvent.Testing`: `FakeAuthService`, `FakeGraphCalendarClient`, fixtures for empty/auth-failure/cancelled/all-day/cross-timezone events, and `FixedTimeProvider`. MSIX install, Widgets Board registration/lifecycle, and WAM interactive sign-in stay as manual validation.
**Why:** Shared fakes prevent duplicated setup across EventShaper and CardBuilder tests while guaranteeing unit and seam tests make no live Graph/MSAL calls.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

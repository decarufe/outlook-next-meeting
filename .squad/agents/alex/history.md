# Project Context

- **Owner:** Eric De Carufel
- **Project:** OutlookNextEvent — widget Windows 11 affiché dans le panneau Widgets (à gauche de la barre des tâches) qui montre les prochains événements/rendez-vous de l'agenda new Outlook.
- **Stack:** C# / .NET, Windows App SDK (widget provider + Adaptive Cards), packaging MSIX, Microsoft Graph (calendrier), MSAL / Entra ID (auth).
- **Created:** 2026-09-22T10:37:31-04:00

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- 2026-09-22T10:37:31-04:00 — Architecture ready in docs\architecture.md; suggested first task: spike MSAL + Graph /me/calendarView in a minimal packaged app; confirm redirect URI, WAM broker, scopes, and consent.
- 2026-09-22T10:56:53-04:00 — See `docs/architecture.md` for MVP architecture; assigned MVP v1 issues: #4, #7, #8, #9.

- 2026-09-22T12:40:46-04:00 — Shipped #7/#8 via PR #17: MsalAuthService and GraphCalendarClient integration boundaries; shipped #9 via PR #19: EventShaper and shaped view model; reviewed and merged Amos's #12 PR #22 and #13 PR #23.

# Project Context

- **Owner:** Eric De Carufel
- **Project:** OutlookNextEvent — widget Windows 11 affiché dans le panneau Widgets (à gauche de la barre des tâches) qui montre les prochains événements/rendez-vous de l'agenda new Outlook.
- **Stack:** C# / .NET, Windows App SDK (widget provider + Adaptive Cards), packaging MSIX, Microsoft Graph (calendrier), MSAL / Entra ID (auth).
- **Created:** 2026-09-22T10:37:31-04:00

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- 2026-09-22T10:37:31-04:00 — Architecture ready in docs\architecture.md; suggested first task: prepare test strategy for event shaping, sorting/time zones, Graph errors, expected JSON rendering, and Auth/Graph fakes.
- 2026-09-22T10:56:53-04:00 — See `docs/architecture.md` for MVP architecture; assigned MVP v1 issues: #12, #13, #14.

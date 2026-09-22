# Project Context

- **Owner:** Eric De Carufel
- **Project:** OutlookNextEvent — widget Windows 11 affiché dans le panneau Widgets (à gauche de la barre des tâches) qui montre les prochains événements/rendez-vous de l'agenda new Outlook.
- **Stack:** C# / .NET, Windows App SDK (widget provider + Adaptive Cards), packaging MSIX, Microsoft Graph (calendrier), MSAL / Entra ID (auth).
- **Created:** 2026-09-22T10:37:31-04:00

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
- 2026-09-22T10:37:31-04:00 — Architecture ready in docs\architecture.md; suggested first task: define Adaptive Card v1 states (
on connecté, liste, ide, rreur) and content constraints for small/large formats.
- 2026-09-22T10:56:53-04:00 — See `docs/architecture.md` for MVP architecture; assigned MVP v1 issues: #10, #11.

- 2026-09-22T12:40:46-04:00 — Shipped #10 via PR #20: CardBuilder and Adaptive Cards v1 states; shipped #11 via PR #21: single-project MSIX dev/sideload packaging and docs; reviewed and merged Holden's #6 PR #18 and Amos's #14 PR #24.

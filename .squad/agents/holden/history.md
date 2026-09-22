# Project Context

- **Owner:** Eric De Carufel
- **Project:** OutlookNextEvent — widget Windows 11 affiché dans le panneau Widgets (à gauche de la barre des tâches) qui montre les prochains événements/rendez-vous de l'agenda new Outlook.
- **Stack:** C# / .NET, Windows App SDK (widget provider + Adaptive Cards), packaging MSIX, Microsoft Graph (calendrier), MSAL / Entra ID (auth).
- **Created:** 2026-09-22T10:37:31-04:00

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

- 2026-09-22T12:40:46-04:00 — Shipped #6 via PR #18: NextEventsWidgetProvider lifecycle, COM activation, widget actions, and host/content/sign-in seams; reviewed and merged Alex's #7/#8 PR #17 plus Naomi's #11 PR #21, verifying the packaging CLSID stayed consistent with the provider.

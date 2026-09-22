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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

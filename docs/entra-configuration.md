# Configuration Microsoft Entra / Outlook

Ce guide explique comment créer l'inscription Microsoft Entra nécessaire pour lire le calendrier Outlook avec Microsoft Graph, puis ce qui reste à brancher dans l'application WinUI/MSIX.

## État actuel / blocage fonctionnel

Créer l'application Entra est nécessaire, mais **ne suffit pas encore à afficher les événements réels dans le widget installé**.

État du code actuel :

- `src\OutlookNextEvent.Infrastructure\Settings\OutlookNextEventSettings.cs` définit `ClientId`, `TenantId`, `Calendars.Read`, la fenêtre calendrier et le redirect URI broker.
- `src\OutlookNextEvent.Infrastructure\Auth\MsalAuthService.cs` sait créer un public client MSAL/WAM avec un HWND parent.
- `src\OutlookNextEvent.Infrastructure\Graph\GraphCalendarClient.cs` sait appeler `/me/calendarView` avec `Prefer: outlook.timezone`.
- `src\OutlookNextEvent.App\Widgets\NextEventsWidgetProviderDependencies.cs` crée encore `NextEventsWidgetContentProvider(new CardBuilder(), eventShaper: new EventShaper())` **sans `ICalendarService`**.
- `src\OutlookNextEvent.App\Widgets\CompanionSignInLauncher.cs` est encore un stub qui retourne `false`; il n'ouvre pas de fenêtre compagnon et n'appelle pas `SignInInteractiveAsync`.
- Il n'existe pas encore de fichier, variable d'environnement, settings Windows ou composition root qui injecte le `ClientId` dans l'application packagée.

Conséquence : le package actuel peut installer le widget, mais il reste sur le squelette/placeholder ou l'état de connexion non branché. Le suivi runtime est l'issue [#27](https://github.com/decarufe/outlook-next-meeting/issues/27) : brancher la configuration Entra, `MsalAuthService`, `GraphCalendarClient` et la fenêtre compagnon.

## 1. Créer l'application dans Microsoft Entra

Dans le portail Azure / Microsoft Entra :

1. Ouvrir **Microsoft Entra ID** > **App registrations** > **New registration**.
2. Nom suggéré : `OutlookNextEvent Dev`.
3. **Supported account types** :
   - pour Outlook.com personnel **et** comptes professionnels/scolaires : choisir **Accounts in any organizational directory and personal Microsoft accounts**;
   - pour un tenant interne seulement : choisir **Accounts in this organizational directory only**;
   - pour plusieurs tenants professionnels/scolaires sans comptes personnels : choisir **Accounts in any organizational directory**.
4. Laisser le redirect URI vide à cette étape si le portail ne propose pas encore la bonne plateforme.
5. Cliquer **Register**.

Le MVP vise par défaut le choix le plus large si vous voulez tester à la fois Outlook.com et comptes de travail/école : **Accounts in any organizational directory and personal Microsoft accounts**.

## 2. Configurer l'authentification desktop/WAM

Dans l'inscription créée :

1. Aller dans **Authentication**.
2. Sélectionner **Add a platform**.
3. Choisir **Mobile and desktop applications**.
4. Ajouter le redirect URI broker WAM actuellement codé :

   ```text
   ms-appx-web://Microsoft.AAD.BrokerPlugin/{CLIENT_ID}
   ```

   Remplacer `{CLIENT_ID}` par l'**Application (client) ID** affiché dans **Overview**.

5. Dans **Advanced settings**, activer **Allow public client flows**.
6. Sauvegarder.

Le code actuel génère exactement cette valeur avec `AuthSettings.CreateBrokerRedirectUri(clientId)`. Exemple de forme attendue :

```text
ms-appx-web://Microsoft.AAD.BrokerPlugin/<Application (client) ID>
```

⚠️ Caveat de validation : le spike `docs\spikes\0004-msix-msal-wam-auth.md` conclut que les docs Microsoft actuelles recommandent cette forme `{client_id}` pour MSAL.NET/WAM, mais que le comportement doit encore être validé dans le package signé sur Windows 11 cible. Si le portail Entra force un flow ou un champ différent pour **Mobile and desktop applications**, conservez la plateforme desktop/WAM et vérifiez contre le spike avant d'ajouter un redirect non documenté. N'ajoutez `http://localhost` ou `https://login.microsoftonline.com/common/oauth2/nativeclient` que si un fallback non-broker est explicitement implémenté et testé.

## 3. Permissions Microsoft Graph

Dans **API permissions** :

1. Cliquer **Add a permission**.
2. Choisir **Microsoft Graph**.
3. Choisir **Delegated permissions**.
4. Ajouter uniquement :

   ```text
   Calendars.Read
   ```

5. Ne pas ajouter de permission **Application** pour le MVP.
6. Ne pas ajouter `Calendars.ReadWrite`.
7. `User.Read` n'est pas requis pour `/me/calendarView`; l'ajouter seulement si une future UI affiche explicitement le compte connecté.

Consentement :

- Un compte personnel ou un tenant permissif peut demander le consentement utilisateur au premier login.
- Certains tenants professionnels bloquent le consentement utilisateur pour `Calendars.Read`; dans ce cas, un administrateur doit utiliser **Grant admin consent** ou approuver la demande selon la politique du tenant.
- Même avec admin consent, l'application reste un public client avec permissions déléguées : elle lit le calendrier de l'utilisateur connecté, pas tous les calendriers du tenant.

## 4. Valeurs à récupérer et stockage sûr

Depuis **Overview**, noter :

- **Application (client) ID** : valeur à fournir à `AuthSettings.ClientId`.
- **Directory (tenant) ID** : utiliser cette valeur pour un tenant précis, ou conserver `common` pour le comportement multi-tenant/personnel déjà prévu par `AuthSettings.DefaultTenantId`.

Ne jamais créer ni stocker de **client secret** pour cette application. OutlookNextEvent est une application desktop/public client avec WAM; un secret applicatif serait inadapté et ne doit pas être committé.

État actuel de configuration runtime :

- Aucun fichier `appsettings`, variable d'environnement ou storage applicatif n'est actuellement lu par `src\OutlookNextEvent.App`.
- Le `ClientId` ne peut donc pas encore être fourni au package installé sans l'implémentation de l'issue [#27](https://github.com/decarufe/outlook-next-meeting/issues/27).
- En attendant, conservez le Client ID et le Tenant ID dans un gestionnaire local sûr ou dans vos notes de développement; ne les ajoutez pas à un fichier source pour "tester vite".

## 5. Installer et tester le package actuel

Depuis la racine du dépôt :

```powershell
dotnet restore .\OutlookNextEvent.sln
dotnet build .\OutlookNextEvent.sln
.\build-package.ps1
```

Puis ouvrir PowerShell **en administrateur** :

```powershell
.\install-package.ps1
```

Vérifier l'installation :

```powershell
Get-AppxPackage -Name Decarufe.OutlookNextEvent
```

Test manuel actuel :

1. Lancer **Outlook Next Event** une fois pour initialiser l'application packagée.
2. Ouvrir le panneau **Widgets** Windows.
3. Ajouter **Prochains événements Outlook**.
4. Cliquer **Actualiser**.
5. Cliquer **Connecter Outlook**.

Résultat attendu aujourd'hui :

- le widget peut afficher le placeholder/squelette ou un état indiquant que la connexion/configuration est requise;
- l'action **Connecter Outlook** ne lance pas encore WAM, car `CompanionSignInLauncher` est un stub;
- aucun événement calendrier réel ne doit être attendu avant l'issue [#27](https://github.com/decarufe/outlook-next-meeting/issues/27).

Résultat attendu une fois le wiring livré :

1. Si la configuration Entra manque : carte explicite **Connexion Outlook requise** ou configuration requise.
2. Si la configuration est présente : le provider tente `AcquireTokenSilent`.
3. Si MSAL demande une interaction : le bouton **Connecter Outlook** ouvre/focus la fenêtre compagnon WinUI.
4. La fenêtre compagnon obtient son HWND et appelle `SignInInteractiveAsync(hwnd)`.
5. Après succès, **Actualiser** relit `/me/calendarView` via Graph et le widget affiche les prochains événements shapés.

## 6. Checklist diagnostic

- **Le widget reste sur le squelette Windows / placeholder** : vérifier que le package est installé, que l'app a été lancée une fois et que le widget a été ajouté depuis le panneau Widgets. Le rendu calendrier réel n'est pas encore branché.
- **"Connecter Outlook" ne fait rien / affiche "Connexion Outlook à finaliser"** : comportement actuel attendu; `CompanionSignInLauncher` n'est pas implémenté.
- **Aucun prompt Microsoft ne s'ouvre** : comportement actuel attendu; la fenêtre compagnon MSAL/WAM n'est pas branchée.
- **Erreur de package non approuvé (`0x800B010A` / `CERT_E_UNTRUSTEDROOT`)** : importer le certificat généré avec `.\install-package.ps1` depuis PowerShell administrateur; voir `docs\packaging.md`.
- **Tenant professionnel demande une approbation admin** : vérifier les politiques de consentement et demander l'approbation de `Calendars.Read` en permission déléguée.
- **Ne pas diagnostiquer avec un client secret** : il ne doit pas y en avoir pour ce type d'application.

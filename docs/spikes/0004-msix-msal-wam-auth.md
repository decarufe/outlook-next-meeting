# Spike #4 — MSIX + MSAL + broker WAM

## Question

Pour une application Win32/.NET packagée MSIX qui affiche les prochains événements Outlook dans un widget Windows 11, quelle configuration MSAL.NET / Entra ID faut-il utiliser pour connecter un utilisateur, obtenir un jeton délégué et lire son calendrier via Microsoft Graph? Le broker Windows WAM doit-il être utilisé, quel redirect URI faut-il enregistrer, et où doit vivre le flux interactif de connexion?

## Findings

### Type d'application et permissions Graph

- L'application est un **public client desktop** : pas de secret applicatif, jeton obtenu au nom de l'utilisateur connecté, permissions **déléguées**. Microsoft documente les applications desktop comme des `IPublicClientApplication` MSAL.NET et indique que l'inscription Entra se configure dans **Authentication > Add a platform > Mobile and desktop applications**. Pour les flux publics, **Allow public client flows** doit être activé dans les réglages avancés. Source : https://learn.microsoft.com/en-us/entra/identity-platform/scenario-desktop-app-configuration
- Pour la lecture calendrier, l'endpoint recommandé reste `GET /me/calendarView?startDateTime={...}&endDateTime={...}` : il renvoie les occurrences, exceptions et instances uniques dans une fenêtre bornée. Les paramètres `startDateTime` et `endDateTime` sont obligatoires, et le header `Prefer: outlook.timezone="..."` contrôle le fuseau des heures retournées. Source : https://learn.microsoft.com/en-us/graph/api/user-list-calendarview?view=graph-rest-1.0
- La page Graph `calendarView` liste `Calendars.ReadBasic` comme permission déléguée la moins privilégiée, et `Calendars.Read` comme permission plus riche. Pour le MVP, je recommande `Calendars.Read` afin de conserver les détails utiles au widget (titre, lieu, lien/réunion si disponible), tout en évitant `Calendars.ReadWrite`. Source : https://learn.microsoft.com/en-us/graph/api/user-list-calendarview?view=graph-rest-1.0
- `User.Read` n'est pas nécessaire pour appeler `calendarView`. Il peut être ajouté seulement si l'UI doit afficher le compte connecté ou si l'expérience produit a besoin de lire le profil. Sinon, rester à `Calendars.Read`.

### WAM / broker Windows

- WAM est le broker Windows intégré. MSAL.NET sait l'utiliser pour bénéficier des comptes connus de Windows, du picker système, de Windows Hello, Conditional Access, FIDO, SSO et de refresh tokens protégés/device-bound. Source : https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam
- Microsoft recommande d'utiliser MSAL.NET **4.52.0+** pour le broker. La prise en charge passe par `Microsoft.Identity.Client` et, dans la plupart des apps desktop modernes, par `Microsoft.Identity.Client.Broker` avec `using Microsoft.Identity.Client.Broker;`. Source : https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam
- Configuration MSAL.NET broker typique :

```csharp
var options = new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
{
    Title = "OutlookNextEvent"
};

IPublicClientApplication pca = PublicClientApplicationBuilder
    .Create(clientId)
    .WithDefaultRedirectUri()
    .WithParentActivityOrWindow(() => hwnd)
    .WithBroker(options)
    .Build();
```

Puis : `AcquireTokenSilent(scopes, account)` d'abord; si `MsalUiRequiredException`, `AcquireTokenInteractive(scopes).WithParentActivityOrWindow(hwnd).ExecuteAsync()`. Sources : https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam et https://learn.microsoft.com/en-us/entra/identity-platform/scenario-desktop-acquire-token-wam
- `WithParentActivityOrWindow` est maintenant requis pour parent-er correctement l'expérience WAM. Pour WinUI 3, l'HWND s'obtient via `WinRT.Interop.WindowNative.GetWindowHandle(window)`. Sources : https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam et https://learn.microsoft.com/en-us/windows/apps/develop/ui/retrieve-hwnd
- WAM exige une session Windows interactive et la capacité d'afficher une UI. Microsoft conseille aussi de donner du contexte avant l'authentification, de déclencher l'auth sur une action utilisateur explicite, et de tenter le silent flow avant l'interactif. Source : https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam

### Redirect URI

- Point important : je n'ai pas trouvé, dans les pages Microsoft Learn actuelles pour MSAL.NET/WAM, de redirect URI broker basé sur un **Package SID** pour cette configuration. Les docs actuelles indiquent de **ne pas configurer le redirect URI WAM dans le code MSAL**, mais de l'enregistrer dans Entra avec la forme :

```text
ms-appx-web://Microsoft.AAD.BrokerPlugin/{client_id}
```

où `{client_id}` est l'**Application (client) ID** de l'app registration Entra. Sources : https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam et https://learn.microsoft.com/en-us/entra/identity-platform/scenario-desktop-acquire-token-wam
- La documentation .NET Azure SDK sur le broker confirme la même valeur pour Windows 10+ / WSL : `ms-appx-web://Microsoft.AAD.BrokerPlugin/{your_client_id}` et précise de remplacer par l'Application (client) ID depuis l'onglet Overview de l'app registration. Source : https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/local-development-broker
- Pour le non-broker / navigateur système : la configuration desktop Microsoft indique `http://localhost` pour les apps utilisant le navigateur système et `https://login.microsoftonline.com/common/oauth2/nativeclient` pour les apps utilisant un navigateur embedded. `WithDefaultRedirectUri()` utilise `http://localhost` sur .NET Core et `https://login.microsoftonline.com/common/oauth2/nativeclient` sur .NET Framework. Sources : https://learn.microsoft.com/en-us/entra/identity-platform/scenario-desktop-app-configuration et https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/default-reply-uri
- Conclusion pratique : pour le MVP packagé MSIX + WAM, enregistrer le redirect broker `ms-appx-web://Microsoft.AAD.BrokerPlugin/{client_id}` sous **Mobile and desktop applications**. Ne pas bloquer #7/#8 sur la génération d'un Package SID tant que la documentation Microsoft actuelle ne l'exige pas.

### Entra app registration

Configuration recommandée :

1. Créer une app registration Entra.
2. Supported account types :
   - si le MVP doit fonctionner pour comptes professionnels/scolaires **et** Outlook.com personnels : choisir **Accounts in any organizational directory and personal Microsoft accounts**;
   - si usage interne seulement : single tenant suffit, mais limite le produit.
3. Authentication > Add a platform > **Mobile and desktop applications**.
4. Ajouter le custom redirect URI broker : `ms-appx-web://Microsoft.AAD.BrokerPlugin/{Application (client) ID}`.
5. Garder aussi, seulement si un fallback non-broker explicite est prévu/testé, `http://localhost` ou `https://login.microsoftonline.com/common/oauth2/nativeclient` selon le mode choisi.
6. Advanced settings > **Allow public client flows = Yes**.
7. API permissions Microsoft Graph : `Calendars.Read`; ajouter `User.Read` uniquement si nécessaire pour l'UI du compte.

Consentement : Microsoft Graph explique que les permissions déléguées peuvent être consenties par l'utilisateur ou par un administrateur selon les politiques du tenant. Les politiques de consentement peuvent restreindre ce que les utilisateurs finaux peuvent accepter; la politique Microsoft recommandée actuelle exclut notamment `Calendars.Read` et `Calendars.ReadBasic` du consentement utilisateur par défaut. Donc certains tenants exigeront admin consent / admin approval même si le flux est un public client délégué. Sources : https://learn.microsoft.com/en-us/graph/permissions-overview et https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/manage-app-consent-policies

### Widget action vs fenêtre compagnon

- Les widgets Windows peuvent recevoir des actions utilisateur via `IWidgetProvider.OnActionInvoked`; Microsoft décrit par exemple un bouton Adaptive Card `Action.Execute` dont le `verb` est traité par le provider. Sources : https://learn.microsoft.com/en-us/windows/apps/develop/widgets/implement-widget-provider-cs et https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.widgets.providers.iwidgetprovider.onactioninvoked?view=windows-app-sdk-2.0
- Mais WAM/MSAL exige une UI affichable et un HWND parent via `WithParentActivityOrWindow`. Un provider de widget peut être activé comme processus de service/COM pour répondre à l'hôte Widgets; il ne donne pas naturellement un contexte visuel fiable ni un HWND utilisateur pour la boîte de dialogue broker.
- Recommandation : le bouton du widget **Connecter Outlook** ne doit pas lancer directement `AcquireTokenInteractive` dans le callback provider. Il doit lancer/focus une petite fenêtre compagnon packagée (WinUI 3) expliquant pourquoi la connexion est nécessaire. Cette fenêtre fournit le HWND à MSAL et exécute le flow interactif WAM. Après succès, le provider repasse en `AcquireTokenSilent` lors des refresh.

## Décision/Recommandation

1. **Broker : oui, par défaut.** Utiliser WAM via MSAL.NET pour l'app MSIX/Win32, car c'est l'expérience Windows recommandée pour SSO, Conditional Access, Windows Hello et protection des tokens.
2. **MSAL : public client + scopes délégués.** Construire un `IPublicClientApplication` avec `WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))`, `WithDefaultRedirectUri()` et `WithParentActivityOrWindow(() => hwnd)` depuis la fenêtre compagnon.
3. **Redirect broker à enregistrer :** `ms-appx-web://Microsoft.AAD.BrokerPlugin/{ApplicationClientId}` dans **Mobile and desktop applications**. Je n'ai pas confirmé de forme actuelle basée sur Package SID dans Microsoft Learn pour MSAL.NET/WAM; les sources Microsoft actuelles utilisent le client ID.
4. **Fallback non-broker :** si nécessaire, enregistrer/tester séparément `http://localhost` pour navigateur système sur .NET Core; ne pas mélanger ce fallback avec le flow broker principal sans tests.
5. **Scopes :** `Calendars.Read` pour le MVP; `User.Read` seulement si on affiche/valide l'identité du compte dans la fenêtre compagnon. Pas de permission application, pas de secret, pas de `Calendars.ReadWrite`.
6. **Lieu de l'interactif :** fenêtre compagnon packagée obligatoire pour le MVP. Le widget affiche l'état et déclenche l'action utilisateur, mais la fenêtre compagnon porte le contexte, le HWND et l'appel `AcquireTokenInteractive`.

## Impact sur #7 GraphCalendarClient et #8 AuthService

### #7 GraphCalendarClient

- Dépendre d'un `AuthService` qui retourne un access token délégué pour `Calendars.Read`.
- Appeler `GET /me/calendarView` avec `startDateTime`/`endDateTime` ISO 8601 bornés, une fenêtre v1 de 7 jours, `$top`/filtrage local si nécessaire, et `Prefer: outlook.timezone="{timezone Windows/IANA choisi}"`.
- Prévoir que certains tenants bloquent le consentement; remonter un état explicite `ConsentRequired/AdminApprovalRequired` plutôt qu'une erreur générique.
- Ne pas implémenter de flow application-only pour le MVP.

### #8 AuthService

- Encapsuler MSAL.NET et exposer deux chemins :
  - `AcquireTokenSilentAsync()` pour provider/widget refresh;
  - `SignInInteractiveAsync(hwnd)` appelé uniquement par la fenêtre compagnon.
- Utiliser `PublicClientApplication.OperatingSystemAccount` comme tentative silencieuse initiale si aucun compte n'est dans le cache, puis fallback interactif.
- Persister le cache MSAL desktop; ne jamais stocker de refresh token manuellement. Microsoft rappelle que les public clients doivent tenter le cache avant une autre méthode et documente la sérialisation du cache. Source : https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization?tabs=desktop
- Gérer `MsalUiRequiredException`, consentement refusé, tenant policy/admin approval, annulation utilisateur et reconnexion/oubli du compte.
- Fournir au widget des états simples : `NotConnected`, `Connected`, `InteractionRequired`, `ConsentBlocked`, `Error`.

## Incertitudes restantes

- Les docs Microsoft Learn actuelles indiquent un redirect broker basé sur `{client_id}`, pas sur Package SID. Si le portail Entra ou un template MSIX spécifique demande encore un Package SID dans une version particulière, il faudra le vérifier avec une app packagée réelle avant #8.
- Le comportement exact de lancement/focus de la fenêtre compagnon depuis le widget doit être validé dans un prototype Windows App SDK, mais la contrainte WAM/HWND suffit pour décider que l'auth interactive ne doit pas vivre dans le callback provider.
- Le choix `Calendars.Read` vs `Calendars.ReadBasic` peut être réévalué après définition exacte des champs affichés par Naomi, mais `Calendars.Read` est le choix sûr pour le MVP actuel.
- Les règles de consentement varient par tenant; prévoir une page/état d'aide pour les environnements professionnels qui exigent une approbation admin.

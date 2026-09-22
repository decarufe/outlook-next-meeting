# Spike #3 — Schéma du manifeste Widget/MSIX

## Question

Identifier le schéma concret `Package.appxmanifest` nécessaire pour enregistrer un provider de widget Windows 11, le modèle d’activation/implémentation `IWidgetProvider` / `IWidgetProvider2`, et produire un snippet minimal pour `NextEventsWidget`.

## Findings

- **Extension MSIX widget : `windows.appExtension` + `uap3:AppExtension`.** Les providers déclarent leurs informations sous `uap3:Extension Category="windows.appExtension"`, avec `uap3:AppExtension Name="com.microsoft.windows.widgets"`. Source : `https://learn.microsoft.com/en-us/windows/apps/develop/widgets/widget-provider-manifest`.
- **Namespace requis : `uap3`.** Le tutoriel C# demande d’ajouter `xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3"` sur l’élément `Package`. Source : `https://learn.microsoft.com/en-us/windows/apps/develop/widgets/implement-widget-provider-cs`.
- **Activation recommandée : `CreateInstance` par COM.** Le manifeste widget contient `<Activation><CreateInstance ClassId="..."/></Activation>`. Microsoft indique que `CreateInstance` est recommandé pour les Win32 providers qui implémentent `IWidgetProvider`; `ActivateApplication` existe mais n’est pas recommandé pour la plupart des providers. Sources : `https://learn.microsoft.com/en-us/windows/apps/develop/widgets/widget-provider-manifest`, `https://learn.microsoft.com/en-us/windows/apps/develop/widgets/widget-provider-activateapplication-protocol`.
- **Extension COM séparée : `windows.comServer`.** Le package doit aussi enregistrer l’exécutable comme serveur COM out-of-process avec `com:Extension Category="windows.comServer"`, `com:ExeServer`, puis `com:Class Id="{CLSID}"`. Le même CLSID est utilisé dans `CreateInstance`. Source : `https://learn.microsoft.com/en-us/windows/apps/develop/widgets/implement-widget-provider-cs`.
- **`IWidgetProvider` est un serveur COM out-of-process.** La référence API précise que `IWidgetProvider` doit être implémenté comme serveur COM out-of-process; les providers d’une même app partagent un process, les apps différentes tournent dans des process séparés. Source : `https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.widgets.providers.iwidgetprovider`.
- **Méthodes de cycle de vie MVP :** `CreateWidget`, `DeleteWidget`, `OnActionInvoked`, `OnWidgetContextChanged`, `Activate`, `Deactivate`. Source : `https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.widgets.providers.iwidgetprovider`.
- **`IWidgetProvider2` est optionnel et sert à la personnalisation.** Il ajoute `OnCustomizationRequested`; il est disponible depuis Windows App SDK 1.4. Il n’est pas requis pour un widget calendrier simple non personnalisable. Sources : `https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.widgets.providers.iwidgetprovider2`, `https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-4`.
- **Définition widget dans le manifeste :** chaque `<Definition>` déclare `Id`, `DisplayName`, `Description`, optionnellement `AllowMultiple`, `IsCustomizable`, régions, URI d’info, etc. Les tailles supportées sont déclarées sous `<Capabilities><Capability><Size Name="small|medium|large"/></Capability>...`. Source : `https://learn.microsoft.com/en-us/windows/apps/develop/widgets/widget-provider-manifest`.
- **Ressources exigées : icônes et screenshots.** `ThemeResources` contient `Icons` et `Screenshots`; `Icon Path=...` et `Screenshot Path=...` sont requis par le format documenté. Source : `https://learn.microsoft.com/en-us/windows/apps/develop/widgets/widget-provider-manifest`.
- **Point important : le manifeste Win32 ne contient pas le template Adaptive Card initial.** Pour un provider Win32, le template et les données JSON sont envoyés par le provider lors de `CreateWidget`/`Activate` via `WidgetManager.GetDefault().UpdateWidget(...)` et `WidgetUpdateRequestOptions.Template/Data`. Le champ `ms_ac_template` appartient au modèle PWA, pas au `Package.appxmanifest` Win32. Sources : tutoriel C# (`https://learn.microsoft.com/en-us/windows/apps/develop/widgets/implement-widget-provider-cs`), docs PWA (`https://learn.microsoft.com/en-us/microsoft-edge/progressive-web-apps/how-to/widgets`).

## Snippet minimal proposé

À adapter avec les vrais noms de fichiers, le nom d’exécutable généré par le projet, et un CLSID stable généré une seule fois pour le provider.

```xml
<Package
  ...
  xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3"
  xmlns:com="http://schemas.microsoft.com/appx/manifest/com/windows10">

  ...
  <Applications>
    <Application
      Id="App"
      Executable="OutlookNextEvent.App\OutlookNextEvent.App.exe"
      EntryPoint="Windows.FullTrustApplication">

      <uap:VisualElements
        DisplayName="Outlook Next Event"
        Description="Affiche les prochains événements Outlook dans Windows Widgets"
        Square44x44Logo="Assets\Square44x44Logo.png"
        Square150x150Logo="Assets\Square150x150Logo.png"
        BackgroundColor="transparent" />

      <Extensions>
        <com:Extension Category="windows.comServer">
          <com:ComServer>
            <com:ExeServer
              Executable="OutlookNextEvent.App\OutlookNextEvent.App.exe"
              DisplayName="Outlook Next Event Widget Provider">
              <com:Class
                Id="00000000-0000-0000-0000-000000000001"
                DisplayName="Outlook Next Event Widget Provider" />
            </com:ExeServer>
          </com:ComServer>
        </com:Extension>

        <uap3:Extension Category="windows.appExtension">
          <uap3:AppExtension
            Name="com.microsoft.windows.widgets"
            DisplayName="Outlook Next Event"
            Id="OutlookNextEventWidgetProvider"
            PublicFolder="Public">
            <uap3:Properties>
              <WidgetProvider>
                <ProviderIcons>
                  <Icon Path="Assets\StoreLogo.png" />
                </ProviderIcons>
                <Activation>
                  <CreateInstance ClassId="00000000-0000-0000-0000-000000000001" />
                </Activation>
                <Definitions>
                  <Definition
                    Id="NextEventsWidget"
                    DisplayName="Prochains événements Outlook"
                    Description="Affiche vos prochains événements du calendrier Outlook."
                    AllowMultiple="false">
                    <Capabilities>
                      <Capability>
                        <Size Name="small" />
                      </Capability>
                      <Capability>
                        <Size Name="medium" />
                      </Capability>
                      <Capability>
                        <Size Name="large" />
                      </Capability>
                    </Capabilities>
                    <ThemeResources>
                      <Icons>
                        <Icon Path="ProviderAssets\NextEventsWidget.png" />
                      </Icons>
                      <Screenshots>
                        <Screenshot
                          Path="ProviderAssets\NextEventsWidgetScreenshot.png"
                          DisplayAltText="Aperçu du widget Prochains événements Outlook" />
                      </Screenshots>
                    </ThemeResources>
                  </Definition>
                </Definitions>
              </WidgetProvider>
            </uap3:Properties>
          </uap3:AppExtension>
        </uap3:Extension>
      </Extensions>
    </Application>
  </Applications>
</Package>
```

### Template initial

Pour Win32, le template initial est **hors manifeste**. Le provider doit l’envoyer dans `CreateWidget` :

```csharp
public void CreateWidget(WidgetContext widgetContext)
{
    var options = new WidgetUpdateRequestOptions(widgetContext.Id)
    {
        Template = NextEventsTemplates.NotConnectedOrLoading,
        Data = "{}",
        CustomState = ""
    };

    WidgetManager.GetDefault().UpdateWidget(options);
}
```

Le template Adaptive Card minimal peut être stocké dans le package, par exemple `Cards\next-events.template.json`, puis lu par le provider. Le manifeste n’a besoin que des métadonnées, tailles et ressources de découverte.

## Décision/Recommandation

1. **Pour #5/#11, générer un package MSIX complet, pas une app unpackaged.**
2. **Utiliser COM `CreateInstance`, pas `ActivateApplication`.** C’est le chemin recommandé par Microsoft pour les providers Win32 `IWidgetProvider`.
3. **Déclarer un seul widget v1 : `NextEventsWidget`, `AllowMultiple="false"`, tailles `small`, `medium`, `large`.**
4. **Ne pas déclarer `IsCustomizable` pour le MVP.** Ajouter `IWidgetProvider2` plus tard seulement si une vraie UI de configuration est décidée.
5. **Ne pas chercher de champ “initial template” dans `Package.appxmanifest`.** Naomi/#6 doivent livrer le template via `WidgetManager.UpdateWidget`.

## Impact sur les issues dépendantes

- **#5 scaffolding** : inclure projet app + projet packaging MSIX + références Windows App SDK; générer un CLSID provider stable; prévoir dossiers `Assets`, `ProviderAssets`, `Cards`.
- **#6 provider** : implémenter `IWidgetProvider`, enregistrer la class factory COM avec le CLSID du manifeste, et envoyer le template/data initial dans `CreateWidget` puis `Activate`.
- **#11 packaging** : valider le manifeste avec les deux extensions (`windows.comServer` et `windows.appExtension`), chemins d’assets valides, signature dev, déploiement local, apparition dans “Add widgets”.

## Incertitudes restantes

- Le snippet doit être validé dans le projet généré, car Visual Studio/MSIX peut ajuster les chemins `Executable` selon le nom du package et du projet.
- Les assets exacts et exigences de tailles d’images doivent être vérifiés pendant #11 contre la page design “Integrate with the widget picker”.
- L’attribut `TrustedPackageFamilyNames` apparaît dans le tutoriel C# mais pas dans la hiérarchie principale de la page manifeste; il n’est pas retenu pour le snippet minimal tant qu’un besoin concret n’est pas identifié.

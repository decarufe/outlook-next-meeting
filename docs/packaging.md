# Packaging MSIX dev/sideload

Ce document décrit le packaging local de `src\OutlookNextEvent.App`, l'application WinUI 3 packagée qui enregistre le widget Windows `NextEventsWidget`.

## Manifeste v1

Le package utilise `src\OutlookNextEvent.App\Package.appxmanifest` avec :

- identité stable : `Decarufe.OutlookNextEvent`, publisher `CN=OutlookNextEventDev`, version `0.1.0.0`;
- app packagée Win32 : `EntryPoint="Windows.FullTrustApplication"`;
- assets visuels : `Assets\StoreLogo.png`, `Assets\Square44x44Logo.png`, `Assets\Square150x150Logo.png`;
- provider COM out-of-process : `windows.comServer`, CLSID `8F3D7C7B-8D0D-41BF-8D9E-608A70D03E95`;
- extension widget : `windows.appExtension`, `Name="com.microsoft.windows.widgets"`;
- activation widget : `<CreateInstance ClassId="8F3D7C7B-8D0D-41BF-8D9E-608A70D03E95" />`;
- définition widget : `NextEventsWidget`, `AllowMultiple="false"`, tailles `small`, `medium`, `large`, icône `ProviderAssets\NextEventsWidget.png`, screenshot `ProviderAssets\NextEventsWidgetScreenshot.png`;
- capabilities : `internetClient` pour Graph et `runFullTrust` car l'app WinUI 3 est déclarée `Windows.FullTrustApplication`.

Le CLSID doit rester synchronisé avec `NextEventsWidgetProvider` et le `WidgetProviderComServer`.

## Prérequis locaux

- Windows 11 avec Widgets Board prenant en charge les widgets tiers.
- Developer Mode activé.
- .NET SDK 8.
- Visual Studio 2022 ou Build Tools avec workloads WinUI/Windows App SDK/MSIX et Windows SDK (`signtool.exe`).

Si une machine headless ne contient pas le workload MSIX/Windows App SDK, `dotnet build` peut compiler les projets mais la génération du package MSIX peut échouer. Dans ce cas, valider Core/Infrastructure/tests et générer le package sur une machine développeur Windows complète.

## Certificat de développement

Ne jamais committer de certificat réel ou de package généré. Le dossier `certs\`, les fichiers `*.pfx`, `*.cer`, `*.pvk`, `*.msix` et `src\OutlookNextEvent.App\Package.local.props` sont ignorés par git.

Option recommandée : générer le certificat, signer le MSIX et copier les artefacts installables à la racine avec le script du dépôt :

```powershell
.\build-package.ps1
```

Le script crée un mot de passe local jetable en mémoire si aucun `-CertificatePassword` n'est fourni, exporte `certs\OutlookNextEventDev.pfx` et `certs\OutlookNextEventDev.cer`, lance `dotnet publish`, puis copie :

- `OutlookNextEvent_0.1.0.0_x64.msix` à la racine du dépôt;
- `OutlookNextEventDev.cer` à la racine du dépôt.

Ces fichiers générés restent ignorés par git.

Exemple avec mot de passe fourni explicitement pour une session locale :

```powershell
.\build-package.ps1 -CertificatePassword "<mot-de-passe-local-jetable>"
```

Option manuelle depuis la racine du dépôt :

```powershell
New-Item -ItemType Directory -Force -Path .\certs | Out-Null
$password = Read-Host "Mot de passe du PFX local" -AsSecureString
$cert = New-SelfSignedCertificate `
  -Type Custom `
  -Subject "CN=OutlookNextEventDev" `
  -FriendlyName "OutlookNextEvent Dev MSIX" `
  -KeyAlgorithm RSA `
  -KeyLength 2048 `
  -KeySpec Signature `
  -HashAlgorithm SHA256 `
  -KeyExportPolicy Exportable `
  -KeyUsage DigitalSignature `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -TextExtension @(
    "2.5.29.37={text}1.3.6.1.5.5.7.3.3",
    "2.5.29.19={text}"
  )

Export-PfxCertificate -Cert $cert -FilePath .\certs\OutlookNextEventDev.pfx -Password $password
Export-Certificate -Cert $cert -FilePath .\certs\OutlookNextEventDev.cer
Import-Certificate -FilePath .\certs\OutlookNextEventDev.cer -CertStoreLocation Cert:\CurrentUser\TrustedPeople
```

Le sujet du certificat doit correspondre au publisher du manifeste : `CN=OutlookNextEventDev`.

## Générer le MSIX

Option recommandée : demander à MSBuild de signer le package avec le certificat local.

```powershell
$plainPassword = Read-Host "Mot de passe du PFX local"
dotnet publish .\src\OutlookNextEvent.App\OutlookNextEvent.App.csproj `
  -c Release `
  -r win-x64 `
  -p:GenerateAppxPackageOnBuild=true `
  -p:UapAppxPackageBuildMode=SideloadOnly `
  -p:AppxBundle=Never `
  -p:PackageCertificateKeyFile="$PWD\certs\OutlookNextEventDev.pfx" `
  -p:PackageCertificatePassword="$plainPassword"
```

Le package est écrit sous `src\OutlookNextEvent.App\MsixPackages\`. Ce dossier est ignoré par git.

Option alternative : générer non signé, puis signer explicitement avec `signtool` :

```powershell
dotnet publish .\src\OutlookNextEvent.App\OutlookNextEvent.App.csproj `
  -c Release `
  -r win-x64 `
  -p:GenerateAppxPackageOnBuild=true `
  -p:UapAppxPackageBuildMode=SideloadOnly `
  -p:AppxBundle=Never `
  -p:AppxPackageSigningEnabled=false

signtool sign /fd SHA256 /f .\certs\OutlookNextEventDev.pfx /p "<mot-de-passe-local>" "<chemin-du-msix>"
```

## Installer et vérifier

```powershell
Import-Certificate -FilePath .\OutlookNextEventDev.cer -CertStoreLocation Cert:\CurrentUser\TrustedPeople
Add-AppxPackage -Path .\OutlookNextEvent_0.1.0.0_x64.msix
Get-AppxPackage -Name Decarufe.OutlookNextEvent
```

Après installation :

1. lancer **Outlook Next Event** une fois pour initialiser le serveur COM du provider;
2. ouvrir le panneau Widgets Windows;
3. choisir **Ajouter des widgets**;
4. vérifier que **Prochains événements Outlook** apparaît avec les tailles `small`, `medium` et `large`;
5. ajouter le widget et vérifier que les actions **Connecter Outlook** / **Actualiser** déclenchent les verbes du provider.

Pour désinstaller :

```powershell
Get-AppxPackage -Name Decarufe.OutlookNextEvent | Remove-AppxPackage
```

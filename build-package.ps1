[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$CertificateSubject = "CN=OutlookNextEventDev",
    [string]$CertificatePassword
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\OutlookNextEvent.App\OutlookNextEvent.App.csproj"
$projectDir = Split-Path -Parent $projectPath
$manifestPath = Join-Path $projectDir "Package.appxmanifest"
$certDir = Join-Path $repoRoot "certs"
$pfxPath = Join-Path $certDir "OutlookNextEventDev.pfx"
$cerPath = Join-Path $certDir "OutlookNextEventDev.cer"
$rootCerPath = Join-Path $repoRoot "OutlookNextEventDev.cer"

if (-not (Test-Path $projectPath)) {
    throw "Project not found: $projectPath"
}

New-Item -ItemType Directory -Force -Path $certDir | Out-Null

if ([string]::IsNullOrWhiteSpace($CertificatePassword)) {
    $passwordBytes = [byte[]]::new(24)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($passwordBytes)
    $CertificatePassword = [Convert]::ToBase64String($passwordBytes)
}

$securePassword = ConvertTo-SecureString -String $CertificatePassword -AsPlainText -Force

Write-Host "Generating local development signing certificate ($CertificateSubject)..."
$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $CertificateSubject `
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
    ) `
    -NotAfter (Get-Date).AddYears(3)

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword -Force | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath -Force | Out-Null

Write-Host "Publishing signed MSIX package..."
$publishArgs = @(
    "publish", $projectPath,
    "-c", $Configuration,
    "-r", $RuntimeIdentifier,
    "-p:GenerateAppxPackageOnBuild=true",
    "-p:UapAppxPackageBuildMode=SideloadOnly",
    "-p:AppxBundle=Never",
    "-p:AppxPackageSigningEnabled=true",
    "-p:PackageCertificateKeyFile=$pfxPath",
    "-p:PackageCertificatePassword=$CertificatePassword"
)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$packageDir = Join-Path $projectDir "MsixPackages"
$msix = Get-ChildItem -Path $packageDir -Filter "*.msix" -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $msix) {
    throw "No .msix package was produced under $packageDir."
}

[xml]$manifest = Get-Content -Path $manifestPath
$version = $manifest.Package.Identity.Version
$platform = if ($RuntimeIdentifier -match "win-(.+)$") { $Matches[1] } else { $RuntimeIdentifier }
$rootMsixPath = Join-Path $repoRoot ("OutlookNextEvent_{0}_{1}.msix" -f $version, $platform)

Copy-Item -Path $msix.FullName -Destination $rootMsixPath -Force
Copy-Item -Path $cerPath -Destination $rootCerPath -Force

$rootMsixItem = Get-Item -Path $rootMsixPath
Write-Host "MSIX copied to: $($rootMsixItem.FullName) ($($rootMsixItem.Length) bytes)"
Write-Host "Certificate copied to: $rootCerPath"

$signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue
if ($signtool) {
    Write-Host "Verifying package signature with signtool..."
    & $signtool.Source verify /pa $rootMsixPath
    if ($LASTEXITCODE -ne 0) {
        throw "signtool verification failed with exit code $LASTEXITCODE."
    }
} else {
    Write-Warning "signtool.exe was not found on PATH; skipped signature verification."
}

Write-Host ""
Write-Host "Install the package from an elevated PowerShell prompt:"
Write-Host ".\install-package.ps1"
Write-Host ""
Write-Host "Manual install commands (run PowerShell as Administrator):"
Write-Host "Import-Certificate -FilePath `"$rootCerPath`" -CertStoreLocation Cert:\LocalMachine\Root"
Write-Host "Import-Certificate -FilePath `"$rootCerPath`" -CertStoreLocation Cert:\LocalMachine\TrustedPeople"
Write-Host "Add-AppxPackage -Path `"$rootMsixPath`""

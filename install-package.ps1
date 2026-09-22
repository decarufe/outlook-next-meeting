[CmdletBinding()]
param(
    [string]$CertificatePath = (Join-Path $PSScriptRoot "OutlookNextEventDev.cer"),
    [string]$PackagePath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Stop-WithMessage {
    param([string]$Message)

    [Console]::Error.WriteLine($Message)
    exit 1
}

if (-not (Test-IsAdministrator)) {
    Stop-WithMessage "Installing the signed MSIX requires machine-level certificate trust. Run PowerShell as Administrator, then rerun .\install-package.ps1."
}

$repoRoot = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $package = Get-ChildItem -Path $repoRoot -Filter "OutlookNextEvent_*_x64.msix" -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $package) {
        Stop-WithMessage "No OutlookNextEvent_*_x64.msix package was found at $repoRoot. Run .\build-package.ps1 first, then rerun .\install-package.ps1."
    }

    $PackagePath = $package.FullName
}

if (-not (Test-Path -LiteralPath $CertificatePath -PathType Leaf)) {
    Stop-WithMessage "Certificate not found: $CertificatePath. Run .\build-package.ps1 first, or pass -CertificatePath <path-to-OutlookNextEventDev.cer>."
}

if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) {
    Stop-WithMessage "MSIX package not found: $PackagePath. Run .\build-package.ps1 first, or pass -PackagePath <path-to-msix>."
}

$resolvedCertPath = (Resolve-Path -LiteralPath $CertificatePath).Path
$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path

Write-Host "Trusting development certificate for MSIX sideloading..."
Import-Certificate -FilePath $resolvedCertPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
Import-Certificate -FilePath $resolvedCertPath -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null

Write-Host "Installing package: $resolvedPackagePath"
Add-AppxPackage -Path $resolvedPackagePath

Write-Host ""
Write-Host "Installation complete. Verification:"
Write-Host "Get-AppxPackage -Name Decarufe.OutlookNextEvent"
Get-AppxPackage -Name Decarufe.OutlookNextEvent |
    Select-Object Name, PackageFullName, Version, InstallLocation

[CmdletBinding()]
param(
    [string]$CodexHome
)

$ErrorActionPreference = 'Stop'

function Resolve-PackageDirectory {
    $sourcePackage = Join-Path (Split-Path -Parent $PSScriptRoot) 'pet\codex'
    $releasePackage = Join-Path $PSScriptRoot 'shenshen'

    if (Test-Path -LiteralPath (Join-Path $sourcePackage 'pet.json')) {
        return (Resolve-Path -LiteralPath $sourcePackage).Path
    }

    if (Test-Path -LiteralPath (Join-Path $releasePackage 'pet.json')) {
        return (Resolve-Path -LiteralPath $releasePackage).Path
    }

    throw 'Codex pet package not found. Run from the repository scripts directory or keep the release package structure intact.'
}

if ([string]::IsNullOrWhiteSpace($CodexHome)) {
    if (-not [string]::IsNullOrWhiteSpace($env:CODEX_HOME)) {
        $CodexHome = $env:CODEX_HOME
    }
    else {
        $CodexHome = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.codex'
    }
}

$packageDirectory = Resolve-PackageDirectory
$codexRoot = [IO.Path]::GetFullPath($CodexHome)
$petsRoot = Join-Path $codexRoot 'pets'
$targetDirectory = Join-Path $petsRoot 'shenshen'

New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null
Copy-Item -LiteralPath (Join-Path $packageDirectory 'pet.json') -Destination $targetDirectory -Force
Copy-Item -LiteralPath (Join-Path $packageDirectory 'spritesheet.webp') -Destination $targetDirectory -Force

Write-Host "Installed Shenshen Codex pet: $targetDirectory"
Write-Host 'In ChatGPT/Codex desktop, open Settings > Pets, select Refresh, choose Shenshen, then enter /pet.'

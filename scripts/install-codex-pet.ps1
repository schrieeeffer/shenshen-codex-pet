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

    throw '找不到 Codex 宠物包。请从仓库 scripts 目录运行，或保持发布压缩包目录结构不变。'
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

Write-Host "已安装深深 Codex 桌宠：$targetDirectory"
Write-Host '请在 ChatGPT/Codex 桌面端打开 设置 > Pets，选择 Refresh 后启用“深深”，再输入 /pet 唤醒。'

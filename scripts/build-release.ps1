[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$RuntimeIdentifier = 'win-x64',
    [string]$DotnetPath = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$distRoot = Join-Path $repoRoot 'dist'
$stagingRoot = Join-Path $distRoot 'release-staging'
$standaloneStage = Join-Path $stagingRoot 'standalone'
$codexStage = Join-Path $stagingRoot 'codex'
$standaloneZip = Join-Path $distRoot 'ShenshenPet-Windows-x64.zip'
$codexZip = Join-Path $distRoot 'Shenshen-Codex-Pet.zip'
$webSource = Join-Path $repoRoot 'pet\web\spritesheet.webp'
$webAsset = Join-Path $distRoot 'Shenshen-ChatGPT-Web-Pet.webp'
$checksumFile = Join-Path $distRoot 'SHA256SUMS.txt'

function Assert-ChildPath {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [string]$Root
    )

    $resolvedRoot = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside the expected directory: $resolvedPath"
    }
}

function Remove-ExistingChild {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [string]$Root
    )

    Assert-ChildPath -Path $Path -Root $Root
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

New-Item -ItemType Directory -Force -Path $distRoot | Out-Null
Remove-ExistingChild -Path $stagingRoot -Root $distRoot
Remove-ExistingChild -Path $standaloneZip -Root $distRoot
Remove-ExistingChild -Path $codexZip -Root $distRoot
Remove-ExistingChild -Path $webAsset -Root $distRoot
Remove-ExistingChild -Path $checksumFile -Root $distRoot
New-Item -ItemType Directory -Force -Path $standaloneStage,$codexStage | Out-Null

$windowsProject = Join-Path $repoRoot 'src\ShenshenPet.Windows\ShenshenPet.Windows.csproj'
# The outer ZIP handles download compression. Keeping the .NET bundle itself
# uncompressed avoids a substantial private-memory penalty at runtime.
& $DotnetPath publish $windowsProject `
    --configuration $Configuration `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    --output $standaloneStage `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=false `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$standalonePayload = @{
    'assets\spritesheet-v2.png' = 'assets\spritesheet-v2.png'
    'pet\pet.manifest.json' = 'pet\pet.manifest.json'
    'codex\pet.json' = 'pet\codex\pet.json'
    'codex\spritesheet.webp' = 'pet\codex\spritesheet.webp'
}
foreach ($entry in $standalonePayload.GetEnumerator()) {
    $destination = Join-Path $standaloneStage $entry.Key
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath (Join-Path $repoRoot $entry.Value) -Destination $destination -Force
}

Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $standaloneStage
Copy-Item -LiteralPath (Join-Path $repoRoot 'ASSET_LICENSE.md') -Destination $standaloneStage
Copy-Item -LiteralPath (Join-Path $repoRoot 'LEGAL_NOTICE.md') -Destination $standaloneStage
Copy-Item -LiteralPath (Join-Path $repoRoot 'AI_PROVENANCE.md') -Destination $standaloneStage
Copy-Item -LiteralPath (Join-Path $repoRoot 'SECURITY.md') -Destination $standaloneStage
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $standaloneStage

$requiredStandaloneFiles = @(
    'ShenshenPet.exe',
    'assets\spritesheet-v2.png',
    'pet\pet.manifest.json',
    'codex\pet.json',
    'codex\spritesheet.webp',
    'LICENSE',
    'ASSET_LICENSE.md',
    'LEGAL_NOTICE.md',
    'AI_PROVENANCE.md',
    'SECURITY.md',
    'README.md'
)
foreach ($relativePath in $requiredStandaloneFiles) {
    $candidate = Join-Path $standaloneStage $relativePath
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "standalone release is missing required file: $relativePath"
    }
}

$selfTest = Start-Process `
    -FilePath (Join-Path $standaloneStage 'ShenshenPet.exe') `
    -ArgumentList '--self-test' `
    -WorkingDirectory $standaloneStage `
    -WindowStyle Hidden `
    -Wait `
    -PassThru
if ($selfTest.ExitCode -ne 0) {
    throw "standalone release self-test failed with exit code $($selfTest.ExitCode)"
}

$codexPetStage = Join-Path $codexStage 'shenshen'
New-Item -ItemType Directory -Force -Path $codexPetStage | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'pet\codex\pet.json') -Destination $codexPetStage
Copy-Item -LiteralPath (Join-Path $repoRoot 'pet\codex\spritesheet.webp') -Destination $codexPetStage
Copy-Item -LiteralPath (Join-Path $repoRoot 'scripts\install-codex-pet.ps1') -Destination $codexStage
Copy-Item -LiteralPath (Join-Path $repoRoot 'README_CODEX.md') -Destination (Join-Path $codexStage 'README.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'ASSET_LICENSE.md') -Destination $codexStage
Copy-Item -LiteralPath (Join-Path $repoRoot 'LEGAL_NOTICE.md') -Destination $codexStage
Copy-Item -LiteralPath (Join-Path $repoRoot 'AI_PROVENANCE.md') -Destination $codexStage

$installerTestHome = Join-Path $stagingRoot 'codex-installer-test-home'
& powershell.exe -NoProfile -ExecutionPolicy Bypass `
    -File (Join-Path $codexStage 'install-codex-pet.ps1') `
    -CodexHome $installerTestHome
if ($LASTEXITCODE -ne 0) {
    throw "Codex installer smoke test failed with exit code $LASTEXITCODE"
}
foreach ($fileName in @('pet.json', 'spritesheet.webp')) {
    $installedFile = Join-Path $installerTestHome "pets\shenshen\$fileName"
    if (-not (Test-Path -LiteralPath $installedFile -PathType Leaf)) {
        throw "Codex installer smoke test is missing $fileName"
    }
}
Remove-ExistingChild -Path $installerTestHome -Root $stagingRoot

Compress-Archive -Path (Join-Path $standaloneStage '*') -DestinationPath $standaloneZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $codexStage '*') -DestinationPath $codexZip -CompressionLevel Optimal

if (-not (Test-Path -LiteralPath $webSource -PathType Leaf)) {
    throw 'web pet asset is missing; run python scripts/build_codex_package.py first'
}
Copy-Item -LiteralPath $webSource -Destination $webAsset
if ((Get-Item -LiteralPath $webAsset).Length -gt 20MB) {
    throw 'ChatGPT web pet exceeds the 20 MiB upload limit'
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$expectedStandaloneEntries = $requiredStandaloneFiles | ForEach-Object { $_.Replace('\', '/') }
$archive = [IO.Compression.ZipFile]::OpenRead($standaloneZip)
try {
    $archiveEntries = $archive.Entries.FullName | ForEach-Object { $_.Replace('\', '/') }
    foreach ($entryName in $expectedStandaloneEntries) {
        if ($entryName -notin $archiveEntries) {
            throw "standalone archive is missing required entry: $entryName"
        }
    }
}
finally {
    $archive.Dispose()
}

$releaseAssets = @($standaloneZip, $codexZip, $webAsset)
$checksumLines = foreach ($asset in $releaseAssets) {
    $hash = (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $(Split-Path -Leaf $asset)"
}
[IO.File]::WriteAllLines($checksumFile, $checksumLines, [Text.UTF8Encoding]::new($false))

Remove-ExistingChild -Path $stagingRoot -Root $distRoot

Write-Host "built: $standaloneZip"
Write-Host "built: $codexZip"
Write-Host "built: $webAsset"
Write-Host "built: $checksumFile"

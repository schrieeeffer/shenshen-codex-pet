[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$RuntimeIdentifier = 'win-x64',
    [string]$DotnetPath = 'dotnet',
    [string]$PythonPath = 'python'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$distRoot = Join-Path $repoRoot 'dist'
$stagingRoot = Join-Path $distRoot 'release-staging'
$portableStage = Join-Path $stagingRoot 'windows-portable'
$runtimeSharedStage = Join-Path $stagingRoot 'windows-runtime-shared'
$codexStage = Join-Path $stagingRoot 'codex'
$bridgeStage = Join-Path $stagingRoot 'bridge'
$petPackStage = Join-Path $stagingRoot 'pet-pack'
$runtimeFramesStage = Join-Path $stagingRoot 'runtime-frames'
$portableZip = Join-Path $distRoot 'ShenshenPet-Windows-x64.zip'
$runtimeSharedZip = Join-Path $distRoot 'ShenshenPet-Windows-x64-runtime-shared.zip'
$legacyLowMemoryZip = Join-Path $distRoot 'ShenshenPet-Windows-x64-low-memory.zip'
$codexZip = Join-Path $distRoot 'Shenshen-Codex-Pet.zip'
$petPackZip = Join-Path $distRoot 'Shenshen-Default-Pet-Pack.zip'
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

function Copy-ApplicationPayload {
    param([Parameter(Mandatory)] [string]$Stage)

    $payload = @{
        'assets\spritesheet-v2.png' = 'assets\spritesheet-v2.png'
        'pet\pet.manifest.json' = 'pet\pet.manifest.json'
        'codex\pet.json' = 'pet\codex\pet.json'
        'codex\spritesheet.webp' = 'pet\codex\spritesheet.webp'
        'codex-bridge\ShenshenPet.Bridge.exe' = 'dist\release-staging\bridge\ShenshenPet.Bridge.exe'
        'codex-bridge\ShenshenPet.Bridge.exe.config' = 'dist\release-staging\bridge\ShenshenPet.Bridge.exe.config'
    }
    foreach ($entry in $payload.GetEnumerator()) {
        $destination = Join-Path $Stage $entry.Key
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
        Copy-Item -LiteralPath (Join-Path $repoRoot $entry.Value) -Destination $destination -Force
    }

    foreach ($fileName in @(
        'LICENSE',
        'ASSET_LICENSE.md',
        'LEGAL_NOTICE.md',
        'AI_PROVENANCE.md',
        'SECURITY.md',
        'PET_PACK_SPEC.md',
        'README.md'
    )) {
        Copy-Item -LiteralPath (Join-Path $repoRoot $fileName) -Destination $Stage
    }

    Copy-Item -LiteralPath $runtimeFramesStage -Destination (Join-Path $Stage 'assets\frames') -Recurse
}

function Assert-RequiredFiles {
    param(
        [Parameter(Mandatory)] [string]$Stage,
        [Parameter(Mandatory)] [string[]]$RelativePaths,
        [Parameter(Mandatory)] [string]$Label
    )

    foreach ($relativePath in $RelativePaths) {
        $candidate = Join-Path $Stage $relativePath
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            throw "$Label is missing required file: $relativePath"
        }
    }
}

function Assert-ZipEntries {
    param(
        [Parameter(Mandatory)] [string]$ZipPath,
        [Parameter(Mandatory)] [string[]]$RelativePaths
    )

    $archive = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $archiveEntries = $archive.Entries.FullName | ForEach-Object { $_.Replace('\', '/') }
        foreach ($relativePath in $RelativePaths) {
            $entryName = $relativePath.Replace('\', '/')
            if ($entryName -notin $archiveEntries) {
                throw "archive is missing required entry: $entryName"
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

if (-not ('ReleasePrivacyScanner' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Text;

public static class ReleasePrivacyScanner
{
    public static bool ContainsText(string path, string value)
    {
        byte[] data = File.ReadAllBytes(path);
        return Contains(data, Encoding.UTF8.GetBytes(value))
            || Contains(data, Encoding.Unicode.GetBytes(value));
    }

    private static bool Contains(byte[] data, byte[] needle)
    {
        if (needle.Length == 0 || data.Length < needle.Length)
        {
            return false;
        }

        int[] prefix = new int[needle.Length];
        for (int i = 1, matched = 0; i < needle.Length;)
        {
            if (needle[i] == needle[matched])
            {
                prefix[i++] = ++matched;
            }
            else if (matched > 0)
            {
                matched = prefix[matched - 1];
            }
            else
            {
                prefix[i++] = 0;
            }
        }

        for (int i = 0, matched = 0; i < data.Length; i++)
        {
            while (matched > 0 && data[i] != needle[matched])
            {
                matched = prefix[matched - 1];
            }

            if (data[i] == needle[matched] && ++matched == needle.Length)
            {
                return true;
            }
        }

        return false;
    }
}
'@
}

function Assert-NoPrivateBuildPaths {
    param(
        [Parameter(Mandatory)] [string]$Stage,
        [Parameter(Mandatory)] [string[]]$PrivatePaths,
        [Parameter(Mandatory)] [string]$Label
    )

    $stagePrefix = [IO.Path]::GetFullPath($Stage).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    foreach ($file in Get-ChildItem -LiteralPath $Stage -Recurse -File) {
        foreach ($privatePath in $PrivatePaths) {
            if ([ReleasePrivacyScanner]::ContainsText($file.FullName, $privatePath)) {
                $relativePath = $file.FullName.Substring($stagePrefix.Length)
                throw "$Label contains a machine-specific build path in: $relativePath"
            }
        }
    }
}

if (Test-Path -LiteralPath $DotnetPath -PathType Leaf) {
    $dotnetExecutable = (Resolve-Path -LiteralPath $DotnetPath).Path
}
else {
    $dotnetExecutable = (Get-Command $DotnetPath -ErrorAction Stop).Source
}

New-Item -ItemType Directory -Force -Path $distRoot | Out-Null
Remove-ExistingChild -Path $stagingRoot -Root $distRoot
foreach ($asset in @($portableZip, $runtimeSharedZip, $legacyLowMemoryZip, $codexZip, $petPackZip, $webAsset, $checksumFile)) {
    Remove-ExistingChild -Path $asset -Root $distRoot
}
New-Item -ItemType Directory -Force -Path $portableStage,$runtimeSharedStage,$codexStage,$bridgeStage,$petPackStage | Out-Null

& $PythonPath (Join-Path $repoRoot 'scripts\build_runtime_frames.py') --output $runtimeFramesStage
if ($LASTEXITCODE -ne 0) {
    throw "runtime frame build failed with exit code $LASTEXITCODE"
}

$windowsProject = Join-Path $repoRoot 'src\ShenshenPet.Windows\ShenshenPet.Windows.csproj'
$bridgeProject = Join-Path $repoRoot 'src\ShenshenPet.Bridge\ShenshenPet.Bridge.csproj'

# The Hook helper targets the Windows-inbox .NET Framework and is only 8 KiB.
# It runs asynchronously, drains stdin, and persists only a predefined state.
& $dotnetExecutable publish $bridgeProject `
    --configuration $Configuration `
    --output $bridgeStage `
    -p:DebugType=None `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) {
    throw "Codex bridge publish failed with exit code $LASTEXITCODE"
}
& (Join-Path $bridgeStage 'ShenshenPet.Bridge.exe') --self-test
if ($LASTEXITCODE -ne 0) {
    throw "Codex bridge self-test failed with exit code $LASTEXITCODE"
}

# Keep the compatibility download self-contained and uncompressed internally.
# The outer ZIP handles download compression; avoiding bundle compression prevents
# the runtime from keeping extracted assemblies in additional private memory.
& $dotnetExecutable publish $windowsProject `
    --configuration $Configuration `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    --output $portableStage `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=false `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) {
    throw "self-contained publish failed with exit code $LASTEXITCODE"
}

# This smaller download reuses the installed .NET Desktop Runtime. Runtime-sharing
# reduces download/disk size; measured steady-state memory is similar to portable.
& $dotnetExecutable publish $windowsProject `
    --configuration $Configuration `
    --runtime $RuntimeIdentifier `
    --self-contained false `
    --output $runtimeSharedStage `
    -p:PublishSingleFile=false `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) {
    throw "runtime-shared publish failed with exit code $LASTEXITCODE"
}

Copy-ApplicationPayload -Stage $portableStage
Copy-ApplicationPayload -Stage $runtimeSharedStage

$privateBuildPaths = @($repoRoot, $env:USERPROFILE, $env:LOCALAPPDATA, $env:TEMP) |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    Select-Object -Unique
Assert-NoPrivateBuildPaths -Stage $portableStage -PrivatePaths $privateBuildPaths -Label 'self-contained release'
Assert-NoPrivateBuildPaths -Stage $runtimeSharedStage -PrivatePaths $privateBuildPaths -Label 'runtime-shared release'

$commonRequiredFiles = @(
    'ShenshenPet.exe',
    'assets\spritesheet-v2.png',
    'assets\frames\0-0.png',
    'assets\frames\10-7.png',
    'pet\pet.manifest.json',
    'codex\pet.json',
    'codex\spritesheet.webp',
    'codex-bridge\ShenshenPet.Bridge.exe',
    'codex-bridge\ShenshenPet.Bridge.exe.config',
    'LICENSE',
    'ASSET_LICENSE.md',
    'LEGAL_NOTICE.md',
    'AI_PROVENANCE.md',
    'SECURITY.md',
    'PET_PACK_SPEC.md',
    'README.md'
)
Assert-RequiredFiles -Stage $portableStage -RelativePaths $commonRequiredFiles -Label 'self-contained release'
$runtimeSharedRequiredFiles = $commonRequiredFiles + @(
    'ShenshenPet.dll',
    'ShenshenPet.Core.dll',
    'ShenshenPet.deps.json',
    'ShenshenPet.runtimeconfig.json'
)
Assert-RequiredFiles -Stage $runtimeSharedStage -RelativePaths $runtimeSharedRequiredFiles -Label 'runtime-shared release'

$selfTest = Start-Process `
    -FilePath (Join-Path $portableStage 'ShenshenPet.exe') `
    -ArgumentList '--self-test' `
    -WorkingDirectory $portableStage `
    -WindowStyle Hidden `
    -Wait `
    -PassThru
if ($selfTest.ExitCode -ne 0) {
    throw "self-contained release self-test failed with exit code $($selfTest.ExitCode)"
}

$runtimeSharedDll = Join-Path $runtimeSharedStage 'ShenshenPet.dll'
$runtimeSharedSelfTest = Start-Process `
    -FilePath $dotnetExecutable `
    -ArgumentList @("`"$runtimeSharedDll`"", '--self-test') `
    -WorkingDirectory $runtimeSharedStage `
    -WindowStyle Hidden `
    -Wait `
    -PassThru
if ($runtimeSharedSelfTest.ExitCode -ne 0) {
    throw "runtime-shared release self-test failed with exit code $($runtimeSharedSelfTest.ExitCode)"
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

Copy-Item -LiteralPath (Join-Path $repoRoot 'pet\pet.manifest.json') -Destination $petPackStage
New-Item -ItemType Directory -Force -Path (Join-Path $petPackStage 'assets') | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets\spritesheet-v2.png') -Destination (Join-Path $petPackStage 'assets')
foreach ($fileName in @('ASSET_LICENSE.md', 'LEGAL_NOTICE.md', 'AI_PROVENANCE.md', 'PET_PACK_SPEC.md')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $fileName) -Destination $petPackStage
}

Compress-Archive -Path (Join-Path $portableStage '*') -DestinationPath $portableZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $runtimeSharedStage '*') -DestinationPath $runtimeSharedZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $codexStage '*') -DestinationPath $codexZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $petPackStage '*') -DestinationPath $petPackZip -CompressionLevel Optimal

if (-not (Test-Path -LiteralPath $webSource -PathType Leaf)) {
    throw 'web pet asset is missing; run python scripts/build_codex_package.py first'
}
Copy-Item -LiteralPath $webSource -Destination $webAsset
if ((Get-Item -LiteralPath $webAsset).Length -gt 20MB) {
    throw 'ChatGPT web pet exceeds the 20 MiB upload limit'
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
Assert-ZipEntries -ZipPath $portableZip -RelativePaths $commonRequiredFiles
Assert-ZipEntries -ZipPath $runtimeSharedZip -RelativePaths $runtimeSharedRequiredFiles
Assert-ZipEntries -ZipPath $petPackZip -RelativePaths @('pet.manifest.json', 'assets\spritesheet-v2.png', 'PET_PACK_SPEC.md')

$releaseAssets = @($portableZip, $runtimeSharedZip, $codexZip, $petPackZip, $webAsset)
$checksumLines = foreach ($asset in $releaseAssets) {
    $hash = (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $(Split-Path -Leaf $asset)"
}
[IO.File]::WriteAllLines($checksumFile, $checksumLines, [Text.UTF8Encoding]::new($false))

Remove-ExistingChild -Path $stagingRoot -Root $distRoot

foreach ($asset in @($portableZip, $runtimeSharedZip, $codexZip, $petPackZip, $webAsset, $checksumFile)) {
    Write-Host "built: $asset"
}

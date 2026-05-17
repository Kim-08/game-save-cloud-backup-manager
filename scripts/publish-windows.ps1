param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$RcloneVersion = "current",
    [switch]$SkipRcloneBundle,
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/GameSaveCloudBackup/GameSaveCloudBackup.csproj"
$output = Join-Path $repoRoot "artifacts/publish/windows-$Runtime"

Write-Host "Publishing Game Save Cloud Backup Manager..." -ForegroundColor Cyan
Write-Host "Project: $project"
Write-Host "Output:  $output"

New-Item -ItemType Directory -Force -Path $output | Out-Null

$arguments = @(
    "publish",
    $project,
    "--configuration", $Configuration,
    "--runtime", $Runtime,
    "--output", $output,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "--self-contained", ($(if ($SelfContained) { "true" } else { "false" }))
)

dotnet @arguments

if (-not $SkipRcloneBundle) {
    $rclonePlatform = switch ($Runtime) {
        "win-x64" { "windows-amd64" }
        "win-x86" { "windows-386" }
        "win-arm64" { "windows-arm64" }
        default {
            throw "Unsupported runtime for bundled rclone: $Runtime. Use -SkipRcloneBundle or add a platform mapping."
        }
    }

    if ($RcloneVersion -eq "current") {
        $rcloneFileName = "rclone-current-$rclonePlatform.zip"
        $rcloneZipUrl = "https://downloads.rclone.org/$rcloneFileName"
    }
    else {
        $normalizedRcloneVersion = if ($RcloneVersion.StartsWith("v")) { $RcloneVersion } else { "v$RcloneVersion" }
        $rcloneFileName = "rclone-$normalizedRcloneVersion-$rclonePlatform.zip"
        $rcloneZipUrl = "https://downloads.rclone.org/$normalizedRcloneVersion/$rcloneFileName"
    }

    $rcloneWork = Join-Path $repoRoot "artifacts/rclone/$Runtime"
    $rcloneZip = Join-Path $rcloneWork $rcloneFileName
    $rcloneExtract = Join-Path $rcloneWork "extract"
    $rcloneOut = Join-Path $output "tools/rclone"

    Write-Host "Downloading rclone: $rcloneZipUrl" -ForegroundColor Cyan
    Remove-Item -Recurse -Force $rcloneExtract -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $rcloneWork, $rcloneExtract, $rcloneOut | Out-Null

    Invoke-WebRequest $rcloneZipUrl -OutFile $rcloneZip

    $rcloneHash = (Get-FileHash $rcloneZip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Host "Downloaded rclone SHA256: $rcloneHash"

    Expand-Archive $rcloneZip -DestinationPath $rcloneExtract -Force

    $rcloneExe = Get-ChildItem $rcloneExtract -Recurse -Filter "rclone.exe" | Select-Object -First 1
    if (-not $rcloneExe) {
        throw "Downloaded rclone archive did not contain rclone.exe."
    }

    Copy-Item $rcloneExe.FullName (Join-Path $rcloneOut "rclone.exe") -Force

    $licenseFile = Get-ChildItem $rcloneExtract -Recurse -Filter "COPYING" | Select-Object -First 1
    if ($licenseFile) {
        Copy-Item $licenseFile.FullName (Join-Path $rcloneOut "COPYING") -Force
    }

    Write-Host "Bundled rclone: $(Join-Path $rcloneOut 'rclone.exe')" -ForegroundColor Green
}
else {
    Write-Host "Skipped rclone bundling. Target machines must provide rclone in PATH." -ForegroundColor Yellow
}

Write-Host "Publish complete: $output" -ForegroundColor Green

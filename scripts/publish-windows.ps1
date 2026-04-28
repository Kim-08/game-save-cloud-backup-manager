param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
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

Write-Host "Publish complete: $output" -ForegroundColor Green
Write-Host "Reminder: rclone is still user-installed and must be available in PATH on the target machine. Tiny dependency goblin, very important."

#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$ArtifactsCore = Join-Path $RepoRoot "benchmarks\artifacts\core"
$ArtifactsGodot = Join-Path $RepoRoot "benchmarks\artifacts\godot"
$ReportDir = Join-Path $RepoRoot "benchmarks\report"

New-Item -ItemType Directory -Force -Path $ArtifactsCore, $ArtifactsGodot, $ReportDir | Out-Null

Write-Host "==> Building solution (Debug for Godot host, Release for Core benchmarks)"
dotnet build DotPudicaFramework.sln --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build benchmarks\DotPudica.Benchmarks\DotPudica.Benchmarks.csproj -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Running Core BenchmarkDotNet"
dotnet run -c Release --project benchmarks\DotPudica.Benchmarks\DotPudica.Benchmarks.csproj -- --filter "*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $env:GODOT_BIN) {
    Write-Error "GODOT_BIN is not set. Point it at Godot 4.7.1 .NET console executable."
}

Write-Host "==> Running Godot headless benchmarks"
& $env:GODOT_BIN --headless --path . res://tests/DotPudica.Integration/Benchmarks/BenchmarkRunner.tscn
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Generating report"
& (Join-Path $PSScriptRoot "generate-report.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. See benchmarks\report\RESULTS.md"

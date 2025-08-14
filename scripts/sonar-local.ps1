# scripts/sonar-local.ps1
param(
  [string]$SolutionPath  # e.g. "IncidentReportingSystem\IncidentReportingSystem.sln"
)

$ErrorActionPreference = "Stop"

# 0) Preconditions
if (-not $env:SONAR_HOST_URL) { throw "SONAR_HOST_URL env var is missing." }
if (-not $env:SONAR_TOKEN)    { throw "SONAR_TOKEN env var is missing." }

# 1) Resolve repo root (parent of /scripts)
$repoRoot = Resolve-Path "$PSScriptRoot\.."

# 2) Locate .sln (support explicit path or recursive search)
if ($SolutionPath) {
  $sln = Get-Item -LiteralPath (Join-Path $repoRoot $SolutionPath) -ErrorAction Stop
} else {
  $solutions = Get-ChildItem -Path $repoRoot -Recurse -Filter *.sln -ErrorAction SilentlyContinue
  if (-not $solutions) { throw "No .sln found under $repoRoot" }

  # Prefer a solution named IncidentReportingSystem.sln if present
  $preferred = $solutions | Where-Object { $_.Name -ieq "IncidentReportingSystem.sln" } | Select-Object -First 1
  $sln = if ($preferred) { $preferred } else { $solutions | Select-Object -First 1 }
}

Write-Host "Using solution: $($sln.FullName)"

# 3) Ensure dotnet-sonarscanner is available
dotnet tool update -g dotnet-sonarscanner | Out-Null
$toolsPath = Join-Path $env:USERPROFILE ".dotnet\tools"
if (-not ($env:PATH -split ";" | Where-Object { $_ -eq $toolsPath })) {
  $env:PATH = "$env:PATH;$toolsPath"
}

# 4) Cleanup previous run (optional)
$sonarWork = Join-Path $repoRoot ".sonarqube"
if (Test-Path $sonarWork) { Remove-Item $sonarWork -Recurse -Force }

# 5) Move to solution directory and run begin→build/test→end
$solutionDir = Split-Path $sln.FullName -Parent
Set-Location $solutionDir

# Use absolute report paths so they are found regardless of CWD
$unitReport = Join-Path $repoRoot "coverage.unit.opencover.xml"
$intReport  = Join-Path $repoRoot "coverage.integration.opencover.xml"

$projectKey = [System.IO.Path]::GetFileNameWithoutExtension($sln.Name)
Write-Host "==> Sonar BEGIN for $projectKey"

dotnet sonarscanner begin `
  /k:"$projectKey" `
  /d:sonar.host.url="$env:SONAR_HOST_URL" `
  /d:sonar.login="$env:SONAR_TOKEN" `
  /d:sonar.cs.opencover.reportsPaths="$unitReport,$intReport"

try {
  Write-Host "==> dotnet build"
  dotnet build $sln.FullName --configuration Debug /t:Rebuild

  Write-Host "==> run tests with coverage"
  & "$PSScriptRoot\run-tests-with-coverage.ps1"

} finally {
  Write-Host "==> Sonar END"
  dotnet sonarscanner end /d:sonar.login="$env:SONAR_TOKEN"
}

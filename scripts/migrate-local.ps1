param(
  [string]$Conn,
  [string]$Context = "ApplicationDbContext"
)

function Get-ConnFromAppSettings([string]$dir, [string]$fileName) {
  $p = Join-Path $dir $fileName
  if (Test-Path $p) {
    $json = Get-Content $p -Raw | ConvertFrom-Json
    if ($json.ConnectionStrings -and $json.ConnectionStrings.DefaultConnection) {
      return $json.ConnectionStrings.DefaultConnection
    }
  }
  return $null
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ApiProj  = Join-Path $RepoRoot "IncidentReportingSystem\IncidentReportingSystem.API"
$InfraProj= Join-Path $RepoRoot "IncidentReportingSystem\IncidentReportingSystem.Infrastructure"

if (-not $Conn) { $Conn = Get-ConnFromAppSettings $ApiProj "appsettings.Development.json" }
if (-not $Conn) { $Conn = Get-ConnFromAppSettings $ApiProj "appsettings.json" }
if (-not $Conn) { $Conn = $env:ConnectionStrings__DefaultConnection }

if (-not $Conn) {
  Write-Error "DefaultConnection not found. Provide -Conn, or set ConnectionStrings__DefaultConnection env var, or put it in appsettings.Development.json."
  exit 1
}

$env:ConnectionStrings__DefaultConnection = $Conn

dotnet ef database update `
  --project "$InfraProj" `
  --startup-project "$ApiProj" `
  --context $Context

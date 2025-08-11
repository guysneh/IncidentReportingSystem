[CmdletBinding()]
param(
  [string]$Context = "ApplicationDbContext"
)

# Repo root = parent of the scripts folder
$root  = (Resolve-Path "$PSScriptRoot\..").Path
$Api   = Join-Path $root "IncidentReportingSystem\IncidentReportingSystem.API"
$Infra = Join-Path $root "IncidentReportingSystem\IncidentReportingSystem.Infrastructure"

$settingsPath = Join-Path $Api "appsettings.Development.json"
if (-not (Test-Path $settingsPath)) { Write-Error "Not found: $settingsPath"; exit 1 }

$conf = Get-Content $settingsPath -Raw | ConvertFrom-Json
$conn = $conf.ConnectionStrings.DefaultConnection
if ([string]::IsNullOrWhiteSpace($conn)) { Write-Error "DefaultConnection not found in appsettings.Development.json"; exit 1 }

# DesignTime factory reads this env var
$env:ConnectionStrings__Default = $conn
Write-Host "Using ConnectionStrings__Default = $($conn -replace 'Password=[^;]*','Password=***')"

dotnet ef database update `
  --project $Infra `
  --startup-project $Api `
  --context $Context

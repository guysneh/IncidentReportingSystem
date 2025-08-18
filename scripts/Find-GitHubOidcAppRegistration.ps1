param(
  [Parameter(Mandatory=$true)][string]$GitHubOwner,   # e.g. "Vatenfall" (org/user)
  [Parameter(Mandatory=$true)][string]$GitHubRepo,    # e.g. "IncidentReportingSystem"
  [string]$Branch                                     # optional, e.g. "main"
)

# Ensure Azure CLI is available
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
  throw "Azure CLI (az) is not installed."
}

Write-Host "==> Checking Azure login..." -ForegroundColor Cyan
try { $null = az account show -o none } catch { az login | Out-Null }

$needle = "repo:$GitHubOwner/$GitHubRepo"
if ($Branch) { $needle = "$needle:ref:refs/heads/$Branch" }

Write-Host "Searching for App Registrations with federated credentials matching: $needle" -ForegroundColor Cyan

# Pull all app registrations (can take a bit in large tenants)
$appListJson = az ad app list --all -o json
$apps = $appListJson | ConvertFrom-Json

$matches = @()

foreach ($app in $apps) {
  $appId = $app.appId
  try {
    $fedsJson = az ad app federated-credential list --id $appId -o json
    if (-not $fedsJson) { continue }
    $feds = $fedsJson | ConvertFrom-Json
    foreach ($fc in $feds) {
      $issuer = $fc.issuer
      $subject = $fc.subject
      if ($issuer -eq "https://token.actions.githubusercontent.com" -and $subject -like "*repo:$GitHubOwner/$GitHubRepo*") {
        if ($Branch) {
          if ($subject -like "*ref:refs/heads/$Branch") {
            $matches += [pscustomobject]@{
              DisplayName = $app.displayName
              ApplicationClientId = $app.appId
              ObjectId = $app.id
              FederatedSubject = $subject
            }
          }
        } else {
          $matches += [pscustomobject]@{
            DisplayName = $app.displayName
            ApplicationClientId = $app.appId
            ObjectId = $app.id
            FederatedSubject = $subject
          }
        }
      }
    }
  } catch {
    # ignore apps without permissions to read federated creds
  }
}

if ($matches.Count -eq 0) {
  Write-Host "No matching App Registration with GitHub OIDC federated credentials found." -ForegroundColor Yellow
  Write-Host "Tip: Try without -Branch, or verify the GitHub repo/org names."
  exit 1
}

Write-Host "`n==> Matching App Registrations:" -ForegroundColor Green
$matches | Format-Table -AutoSize

# Emit a simple line you can copy-paste
$cid = $matches[0].ApplicationClientId
Write-Host "`nUse this client ID for the bootstrap script:" -ForegroundColor Green
Write-Host $cid -ForegroundColor Magenta

param(
  [string]$SubscriptionId,
  [string]$ResourceGroup      = "incident-rg",
  [string]$WebAppName         = "incident-api",
  [string]$AppConfigName      = "incident-appcfg",
  [string]$TerraformSpClientId,   # <-- paste the Application (client) ID found in step 1
  [switch]$GrantOwnerAtRG
)

if (-not (Get-Command az -ErrorAction SilentlyContinue)) { throw "Azure CLI (az) is not installed." }

try { $null = az account show -o none } catch { az login | Out-Null }
if ($SubscriptionId) { az account set --subscription $SubscriptionId }
$SubId = az account show --query id -o tsv

$AppCfgId = az appconfig show -g $ResourceGroup -n $AppConfigName --query id -o tsv
if (-not $AppCfgId) { throw "App Configuration '$AppConfigName' not found in RG '$ResourceGroup'." }

$WebAppMiObjectId = az webapp show -g $ResourceGroup -n $WebAppName --query identity.principalId -o tsv
if (-not $WebAppMiObjectId) { throw "Web App '$WebAppName' not found or has no managed identity." }

if (-not $TerraformSpClientId) { throw "Missing -TerraformSpClientId (Application client ID of the TF SP)." }
$TfSpObjectId = az ad sp show --id $TerraformSpClientId --query id -o tsv
if (-not $TfSpObjectId) { throw "Service Principal with client id '$TerraformSpClientId' not found." }

$RgId = "/subscriptions/$SubId/resourceGroups/$ResourceGroup"

function Ensure-RoleAssignment {
  param([string]$Scope,[string]$RoleName,[string]$AssigneeObjectId,[string]$AssigneePrincipalType = "ServicePrincipal")
  $exists = az role assignment list --assignee-object-id $AssigneeObjectId --scope $Scope --query "[?roleDefinitionName=='$RoleName'] | length(@)" -o tsv
  if ($exists -eq "0") {
    az role assignment create --assignee-object-id $AssigneeObjectId --assignee-principal-type $AssigneePrincipalType --role "$RoleName" --scope $Scope --only-show-errors | Out-Null
    Write-Host "Granted $RoleName on $Scope" -ForegroundColor Green
  } else {
    Write-Host "Already has $RoleName on $Scope" -ForegroundColor DarkGray
  }
}

Ensure-RoleAssignment -Scope $AppCfgId -RoleName "App Configuration Data Reader" -AssigneeObjectId $WebAppMiObjectId
Ensure-RoleAssignment -Scope $AppCfgId -RoleName "App Configuration Data Owner" -AssigneeObjectId $TfSpObjectId

if ($GrantOwnerAtRG) {
  Ensure-RoleAssignment -Scope $RgId -RoleName "Owner" -AssigneeObjectId $TfSpObjectId
} else {
  Ensure-RoleAssignment -Scope $RgId -RoleName "Contributor" -AssigneeObjectId $TfSpObjectId
  Ensure-RoleAssignment -Scope $RgId -RoleName "User Access Administrator" -AssigneeObjectId $TfSpObjectId
}

Write-Host "RBAC bootstrap completed." -ForegroundColor Green

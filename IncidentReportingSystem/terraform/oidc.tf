variable "github_org" {
  type    = string
  default = "guysneh"
}
variable "github_repo" {
  type    = string
  default = "IncidentReportingSystem"
}
variable "github_branch" {
  type    = string
  default = "main"
}

data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

data "azurerm_linux_web_app" "app" {
  name                = var.app_service_name
  resource_group_name = var.resource_group_name
}

data "azurerm_key_vault" "kv" {
  name                = "incident-kv"
  resource_group_name = var.resource_group_name
}

resource "azuread_application" "gha" {
  display_name = "irs-github-actions-oidc"
}

resource "azuread_service_principal" "gha" {
  client_id = azuread_application.gha.client_id
}

resource "azuread_application_federated_identity_credential" "github_branch" {
  application_id = azuread_application.gha.id
  display_name   = "github-branch-${var.github_branch}"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_org}/${var.github_repo}:ref:refs/heads/${var.github_branch}"
}

resource "azurerm_role_assignment" "gha_rg_contributor" {
  scope                = data.azurerm_resource_group.rg.id
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.gha.object_id
}

resource "azurerm_role_assignment" "gha_kv_secrets_user" {
  scope                = data.azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azuread_service_principal.gha.object_id
}

output "gha_client_id" { value = azuread_application.gha.client_id }
output "tenant_id" { value = data.azurerm_client_config.current.tenant_id }
output "subscription_id" {
  value = data.azurerm_client_config.current.subscription_id
}
output "resource_group_name" { value = data.azurerm_resource_group.rg.name }
output "webapp_name" { value = data.azurerm_linux_web_app.app.name }
output "object_id" {
  value = data.azurerm_client_config.current.object_id
}
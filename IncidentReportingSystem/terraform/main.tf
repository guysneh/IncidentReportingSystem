module "resource_group" {
  source              = "./modules/resource_group"
  resource_group_name = var.resource_group_name
  location            = var.location
  default_tags        = var.default_tags
}

module "app_service_plan" {
  source              = "./modules/app_service_plan"
  name                = "incident-app-plan"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_name            = var.app_service_plan_sku_name
  worker_count        = 1
  default_tags        = var.default_tags
}

module "postgres" {
  source                        = "./modules/postgres"
  postgresql_server_name        = "incident-db"
  resource_group_name           = module.resource_group.name
  location                      = module.resource_group.location
  db_admin_username             = var.db_admin_username
  public_network_access_enabled = true
  allow_all_azure               = true
  key_vault_id                  = module.key_vault.id
  tags                          = var.default_tags
}

module "key_vault" {
  source              = "./modules/key_vault"
  name                = "incident-kv"
  location            = var.location
  resource_group_name = module.resource_group.name
  tags                = var.default_tags

  ci_principal_object_id  = azuread_service_principal.gha.object_id
  ci_role_assignment_name = var.ci_role_assignment_name

  secrets = {
    jwt-issuer         = var.jwt_issuer
    jwt-audience       = var.jwt_audience
    jwt-expiry-minutes = tostring(var.jwt_expiry_minutes)
  }
}

module "app_service" {
  source                            = "./modules/app_service"
  name                              = "${var.name_prefix}-api"
  app_service_plan_id               = module.app_service_plan.id
  resource_group_name               = module.resource_group.name
  location                          = var.location
  health_check_path                 = "/health"
  health_check_eviction_time_in_min = 5
  app_settings                      = local.app_settings
  tags                              = var.tags
}

locals {
  webapp_outbound_ips = toset(split(",", module.app_service.outbound_ip_addresses))
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "webapp_outbound" {
  for_each         = local.webapp_outbound_ips
  name             = "webapp-outbound-${replace(each.value, ".", "-")}"
  server_id        = module.postgres.server_id
  start_ip_address = each.value
  end_ip_address   = each.value
}

module "monitoring" {
  source = "./modules/monitoring"

  name_prefix         = var.name_prefix
  resource_group_name = module.resource_group.name
  location            = var.location
  tags                = var.tags
  ai_retention_days   = var.ai_retention_days
  action_group_name   = "incident-action-group"
  action_group_email  = var.action_group_email
}

module "app_configuration" {
  source              = "./modules/app_configuration"
  name                = var.app_config_name
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  tags                = var.default_tags

  app_name     = var.app_name
  api_basepath = var.api_basepath
  api_version  = var.api_version

  enable_swagger           = true
  demo_enable_config_probe = var.demo_enable_config_probe
  demo_probe_auth_mode     = var.demo_probe_auth_mode

  stabilization_delay    = var.stabilization_delay
  default_tags           = var.default_tags
  web_app_name           = var.web_app_name
  app_config_name        = var.app_config_name
  app_config_label       = var.app_config_label
  telemetry_sample_ratio = var.telemetry_sample_ratio
  depends_on = [
    time_sleep.appcfg_rbac_propagation
  ]
  webapp_identity_object_id = one(data.azurerm_linux_web_app.api.identity[*].principal_id)
}



resource "azurerm_role_assignment" "webapp_kv_secrets_user" {
  scope                = module.key_vault.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = module.app_service.principal_id
}

locals {
  app_settings = {
    "AppConfig__Enabled"                   = "true"
    "AppConfig__Endpoint"                  = module.app_configuration.endpoint
    "ConnectionStrings__DefaultConnection" = "@Microsoft.KeyVault(SecretUri=https://incident-kv.vault.azure.net/secrets/PostgreSqlConnectionString)"
    "Telemetry__SamplingRatio"             = var.telemetry_sample_ratio
    "Jwt__Secret"                          = "@Microsoft.KeyVault(SecretUri=https://incident-kv.vault.azure.net/secrets/jwt-secret)"
    "AppConfig__Label"                     = var.app_config_label
    "AppConfig__CacheSeconds"              = tostring(var.app_config_cache_seconds)
    "Attachments__Storage"                 = "Azure"
    "Attachments__Container"               = module.storage.container_name
    "Storage__Blob__Endpoint"              = module.storage.blob_endpoint
    "Storage__Blob__AccountName"           = module.storage.account_name
    # "Storage__Blob__PublicEndpoint" CDN/Front Door
  }
}

resource "azurerm_role_assignment" "ci_appcfg_data_owner" {
  scope                = data.azurerm_resource_group.rg.id
  role_definition_name = "App Configuration Data Owner"
  principal_id         = azuread_service_principal.gha.object_id
}

resource "time_sleep" "appcfg_rbac_propagation" {
  create_duration = "60s"
  depends_on      = [azurerm_role_assignment.ci_appcfg_data_owner]
}

data "azurerm_linux_web_app" "api" {
  name                = var.webapp_name
  resource_group_name = var.webapp_resource_group_name
}

module "storage" {
  source              = "./modules/storage"
  name                = "${var.name_prefix}-sa"
  resource_group_name = module.resource_group.name
  location            = var.location
  container_name      = "attachments"
  default_tags        = var.default_tags
}

resource "azurerm_role_assignment" "webapp_blob_contributor" {
  scope                = module.storage.account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_linux_web_app.api.identity[0].principal_id
}

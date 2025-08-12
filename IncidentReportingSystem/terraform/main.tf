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
    jwt-secret         = random_password.jwt_secret.result
    jwt-expiry-minutes = tostring(var.jwt_expiry_minutes)
  }
}



module "app_service" {
  source              = "./modules/app_service"
  name                = "${var.name_prefix}-api"
  app_service_plan_id = module.app_service_plan.id
  resource_group_name = module.resource_group.name
  location            = var.location
  health_check_path = "/health"
  health_check_eviction_time_in_min = 5

  app_settings = local.app_settings
  tags         = var.tags
}

resource "azurerm_role_assignment" "webapp_kv_secrets_user" {
  scope                = module.key_vault.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = module.app_service.principal_id
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
locals {
  # Base settings – single source of truth for the connection string
  base_app_settings = {
    "ConnectionStrings__DefaultConnection" = "@Microsoft.KeyVault(SecretUri=${module.key_vault.uri}secrets/PostgreSqlConnectionString/)"
    "ASPNETCORE_ENVIRONMENT"               = "Production"
    "EnableSwagger"                        = "true"
  }

  # Extra settings – Application Insights for example
  extra_app_settings = {
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = module.monitoring.connection_string
  }

  # Final app settings passed into the module
  app_settings = merge(
    local.base_app_settings,
    local.extra_app_settings,
    {
      # JWT from Key Vault (no duplication)
      "Jwt__Issuer"        = "@Microsoft.KeyVault(SecretUri=${module.key_vault.uri}secrets/jwt-issuer/)"
      "Jwt__Audience"      = "@Microsoft.KeyVault(SecretUri=${module.key_vault.uri}secrets/jwt-audience/)"
      "Jwt__Secret"        = "@Microsoft.KeyVault(SecretUri=${module.key_vault.uri}secrets/jwt-secret/)"
      "Jwt__ExpiryMinutes" = tostring(var.jwt_expiry_minutes)
    }
  )
}

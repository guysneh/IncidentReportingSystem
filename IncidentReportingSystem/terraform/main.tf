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
  location                      = var.location
  resource_group_name           = module.resource_group.name
  db_admin_username             = var.db_admin_username
  db_admin_password             = random_password.postgres_admin.result
  tags                          = var.default_tags
  public_network_access_enabled = true
}



module "key_vault" {
  source              = "./modules/key_vault"
  name                = "incident-kv"
  location            = var.location
  resource_group_name = module.resource_group.name
  tags                = var.default_tags

  secrets = {
    PostgreSqlConnectionString = "Host=${module.postgres.fqdn};Database=postgres;Username=${var.db_admin_username};Password=${random_password.postgres_admin.result};Port=5432;Ssl Mode=Require;Trust Server Certificate=true"
    jwt-issuer                 = var.jwt_issuer
    jwt-audience               = var.jwt_audience
    jwt-secret                 = random_password.jwt_secret.result
    jwt-expiry-minutes         = tostring(var.jwt_expiry_minutes)
  }
}


module "app_service" {
  source              = "./modules/app_service"
  name                = var.app_service_name
  location            = var.location
  resource_group_name = module.resource_group.name
  app_service_plan_id = module.app_service_plan.id
  tags                = var.default_tags

  app_settings = {
    "ConnectionStrings__DefaultConnection" = "@Microsoft.KeyVault(SecretUri=${module.key_vault.uri}secrets/PostgreSqlConnectionString/)"
    "ASPNETCORE_ENVIRONMENT"               = "Production"
    "Jwt__Issuer"                          = "@Microsoft.KeyVault(SecretUri=${module.key_vault.uri}secrets/jwt-issuer/)"
    "Jwt__Audience"                        = "@Microsoft.KeyVault(SecretUri=${module.key_vault.uri}secrets/jwt-audience/)"
    "Jwt__Secret"                          = "@Microsoft.KeyVault(SecretUri=${module.key_vault.uri}secrets/jwt-secret/)"
    "Jwt__ExpiryMinutes"                   = tostring(var.jwt_expiry_minutes)
    "EnableSwagger"                        = "true"
  }
  always_on = true
}

resource "azurerm_role_assignment" "webapp_kv_secrets_user" {
  scope                = module.key_vault.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = module.app_service.principal_id
}

# Resource Group
module "resource_group" {
  source              = "./modules/resource_group"
  resource_group_name = var.resource_group_name
  location            = var.location
  default_tags        = var.default_tags
}

# App Service Plan
module "app_service_plan" {
  source              = "./modules/app_service_plan"
  name                = "incident-app-plan"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_name            = var.app_service_plan_sku_name
  worker_count        = 1
  default_tags        = var.default_tags
}


# PostgreSQL Database
module "postgres" {
  source                 = "./modules/postgres"
  postgresql_server_name = "incident-db"
  location               = var.location
  resource_group_name    = module.resource_group.name
  db_admin_username      = var.db_admin_username
  db_admin_password      = var.db_admin_password
  tags                   = var.default_tags
}


# Key Vault
module "key_vault" {
  source              = "./modules/key_vault"
  name                = "incident-kv"
  location            = var.location
  resource_group_name = module.resource_group.name
  tags                = var.default_tags

  secrets = {
    PostgreSqlConnectionString = var.postgres_connection_string
  }
}


# App Service
module "app_service" {
  source              = "./modules/app_service"
  name                = var.app_service_name
  location            = var.location
  resource_group_name = module.resource_group.name
  app_service_plan_id = module.app_service_plan.id
  tags                = var.default_tags

  app_settings = {
    "ConnectionStrings__Default" = "@Microsoft.KeyVault(SecretUri=${module.key_vault.uri}secrets/PostgreSqlConnectionString/)"
    "ASPNETCORE_ENVIRONMENT"     = "Production"
  }
}

# Lookup the created Web App to retrieve its managed identity principal
data "azurerm_linux_web_app" "app_identity" {
  name                = var.app_service_name
  resource_group_name = module.resource_group.name

  depends_on = [module.app_service]
}

resource "azurerm_role_assignment" "me_kv_secrets_user" {
  scope                = module.key_vault.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = data.azurerm_client_config.current.object_id
}
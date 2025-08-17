resource "azurerm_app_configuration" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "standard"
  tags                = var.tags
}

# Allow the Web App's managed identity to read App Configuration (data-plane role)
resource "azurerm_role_assignment" "webapp_appcfg_reader" {
  scope                = azurerm_app_configuration.this.id
  role_definition_name = "App Configuration Data Reader"
  principal_id         = var.webapp_principal_id
}

# -------- Plain keys (no labels) --------
resource "azurerm_app_configuration_key" "sentinel" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "AppConfig:Sentinel"
  value                  = "1"
  content_type           = "text/plain"
}

resource "azurerm_app_configuration_key" "app_name" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "App:Name"
  value                  = var.app_name
  content_type           = "text/plain"
}

resource "azurerm_app_configuration_key" "api_basepath" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "Api:BasePath"
  value                  = var.api_basepath
  content_type           = "text/plain"
}

resource "azurerm_app_configuration_key" "api_version" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "Api:Version"
  value                  = var.api_version
  content_type           = "text/plain"
}

resource "azurerm_app_configuration_key" "enable_swagger" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "EnableSwagger"
  value                  = tostring(var.enable_swagger)
  content_type           = "text/plain"
}

resource "azurerm_app_configuration_key" "demo_enable_probe" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "Demo:EnableConfigProbe"
  value                  = tostring(var.demo_enable_config_probe)
  content_type           = "text/plain"
}

resource "azurerm_app_configuration_key" "demo_probe_auth_mode" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "Demo:ProbeAuthMode"
  value                  = var.demo_probe_auth_mode
  content_type           = "text/plain"
}

# -------- Key Vault references (App Configuration -> Key Vault) --------
locals {
  kv_uri = trimsuffix(var.key_vault_uri, "/")
}

resource "azurerm_app_configuration_key" "conn_default" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "ConnectionStrings:DefaultConnection"
  content_type           = "application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8"
  value = jsonencode({
    uri = "${local.kv_uri}/secrets/${var.db_conn_secret_name}"
  })
}

resource "azurerm_app_configuration_key" "jwt_issuer" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "Jwt:Issuer"
  content_type           = "application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8"
  value = jsonencode({
    uri = "${local.kv_uri}/secrets/${var.jwt_issuer_secret_name}"
  })
}

resource "azurerm_app_configuration_key" "jwt_audience" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "Jwt:Audience"
  content_type           = "application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8"
  value = jsonencode({
    uri = "${local.kv_uri}/secrets/${var.jwt_audience_secret_name}"
  })
}

resource "azurerm_app_configuration_key" "jwt_secret" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "Jwt:Secret"
  content_type           = "application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8"
  value = jsonencode({
    uri = "${local.kv_uri}/secrets/${var.jwt_secret_secret_name}"
  })
}

resource "azurerm_app_configuration_key" "ai_conn" {
  count                  = length(var.ai_connection_string) > 0 ? 1 : 0
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = "APPLICATIONINSIGHTS_CONNECTION_STRING"
  value                  = var.ai_connection_string
  content_type           = "text/plain"
}

resource "time_sleep" "wait_for_appcfg" {
  create_duration = "90s"
}

resource "azurerm_app_configuration_key" "keys" {
  for_each               = var.appconfig_settings
  configuration_store_id = azurerm_app_configuration.appcfg.id
  key                    = each.key
  value                  = each.value
  label                  = "prod"

  depends_on = [
    azurerm_app_configuration.appcfg,
    time_sleep.wait_for_appcfg
  ]
}

# -------- Feature Flags --------
resource "azurerm_app_configuration_key" "ff_demo_banner" {
  configuration_store_id = azurerm_app_configuration.this.id
  key                    = ".appconfig.featureflag/EnableDemoBanner"
  content_type           = "application/vnd.microsoft.appconfig.ff+json;charset=utf-8"
  value = jsonencode({
    id          = "EnableDemoBanner"
    description = "Show demo banner / enable demo UX hints"
    enabled     = var.feature_enable_demo_banner_default
    conditions  = { client_filters = [] }
  })
}

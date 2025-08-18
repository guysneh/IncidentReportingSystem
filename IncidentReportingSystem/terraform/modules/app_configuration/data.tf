########################################
# Feature Flags (runtime ON/OFF, no restart)
########################################

resource "azurerm_app_configuration_feature" "enable_swagger_ui" {
  configuration_store_id = azurerm_app_configuration.appcfg.id
  name                   = "EnableSwaggerUI"
  enabled                = true
  # label                = ""  # no labels in this project
}

resource "azurerm_app_configuration_feature" "enable_config_probe" {
  configuration_store_id = azurerm_app_configuration.appcfg.id
  name                   = "EnableConfigProbe"
  enabled                = true
}

resource "azurerm_app_configuration_feature" "enable_demo_banner" {
  configuration_store_id = azurerm_app_configuration.appcfg.id
  name                   = "EnableDemoBanner"
  enabled                = true
}

########################################
# Key-Values used by the demo (refreshed via Sentinel)
########################################

variable "demo_probe_auth_mode" {
  description = "Value for Demo:ProbeAuthMode (e.g., Admin or Anonymous)."
  type        = string
  default     = "Admin"
}

variable "app_name_value" {
  description = "Value for App:Name."
  type        = string
  default     = "Incident API"
}

variable "api_version_value" {
  description = "Value for Api:Version."
  type        = string
  default     = "1.0.0"
}

resource "azurerm_app_configuration_key" "kv_demo_probe_auth_mode" {
  configuration_store_id = azurerm_app_configuration.appcfg.id
  key                    = "Demo:ProbeAuthMode"
  value                  = var.demo_probe_auth_mode
  # label                = ""
}

resource "azurerm_app_configuration_key" "kv_app_name" {
  configuration_store_id = azurerm_app_configuration.appcfg.id
  key                    = "App:Name"
  value                  = var.app_name_value
}

resource "azurerm_app_configuration_key" "kv_api_version" {
  configuration_store_id = azurerm_app_configuration.appcfg.id
  key                    = "Api:Version"
  value                  = var.api_version_value
}

########################################
# Sentinel (triggers refresh of Key-Values)
########################################

variable "sentinel_value" {
  description = "Value for AppConfig:Sentinel (bump to trigger refresh)."
  type        = string
  default     = "v1"
}

resource "azurerm_app_configuration_key" "kv_sentinel" {
  configuration_store_id = azurerm_app_configuration.appcfg.id
  key                    = "AppConfig:Sentinel"
  value                  = var.sentinel_value
}

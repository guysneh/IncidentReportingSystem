resource "azurerm_app_configuration" "appcfg" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "standard"
  tags                = var.tags
}

resource "time_sleep" "wait_for_appcfg" {
  create_duration = var.stabilization_delay
  depends_on      = [azurerm_app_configuration.appcfg]
}

locals {
  appconfig_settings = {
    "App:Name"           = var.app_name
    "Api:BasePath"       = var.api_basepath
    "Api:Version"        = var.api_version
    "Demo:ProbeAuthMode" = var.demo_probe_auth_mode
    "AppConfig:Sentinel" = "v1"
  }
}

resource "azurerm_app_configuration_key" "kv_api_basepath" {
  configuration_store_id = azurerm_app_configuration.appcfg.id
  key                    = "Api:BasePath"
  value                  = var.api_basepath
}

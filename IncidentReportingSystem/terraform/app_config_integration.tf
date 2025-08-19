# Existing App Configuration (by name + RG)
data "azurerm_app_configuration" "this" {
  name                = var.app_config_name
  resource_group_name = var.resource_group_name
}

# Existing Web App (by name + RG) - to read MI principal_id
data "azurerm_linux_web_app" "web_api" {
  name                = var.web_app_name
  resource_group_name = var.resource_group_name
}

# Role assignment: App Configuration Data Reader -> API's Managed Identity
resource "azurerm_role_assignment" "appcfg_reader" {
  scope                = data.azurerm_app_configuration.this.id
  role_definition_name = "App Configuration Data Reader"
  principal_id         = data.azurerm_linux_web_app.api.identity[0].principal_id
}

# Seed keys (label-scoped)
resource "azurerm_app_configuration_key" "sentinel" {
  configuration_store_id = data.azurerm_app_configuration.this.id
  key                    = "AppConfig:Sentinel"
  label                  = var.app_config_label
  value                  = "1"
  tags                   = var.default_tags
}

resource "azurerm_app_configuration_key" "sample_ratio" {
  configuration_store_id = data.azurerm_app_configuration.this.id
  key                    = "MyAppSettings:SampleRatio"
  label                  = var.app_config_label
  value                  = "1.0"
  content_type           = "text/plain"
  tags                   = var.default_tags
}

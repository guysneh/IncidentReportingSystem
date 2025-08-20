# Existing App Configuration (by name + RG)
data "azurerm_app_configuration" "this" {
  name                = var.app_config_name
  resource_group_name = var.resource_group_name
}

# Feature flag under the *label* your app uses (e.g., prod)
resource "azurerm_app_configuration_feature" "enable_swagger_ui_prod" {
  configuration_store_id = data.azurerm_app_configuration.this.id
  name                   = "EnableSwaggerUI"
  label                  = var.app_config_label # לדוגמה: "prod"
  enabled                = true
  description            = "Swagger UI gate for labelled environment"
  tags                   = var.default_tags
}


# Existing Web App (by name + RG) - to read MI principal_id
data "azurerm_linux_web_app" "web_api" {
  name                = var.web_app_name
  resource_group_name = var.resource_group_name
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
  value                  = var.telemetry_sample_ratio
  content_type           = "text/plain"
  tags                   = var.default_tags
}

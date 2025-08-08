resource "azurerm_linux_web_app" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = var.app_service_plan_id

  site_config {
    application_stack {
      dotnet_version = "8.0" 
    }

    always_on = true
  }

  https_only = true

  tags = var.tags
}

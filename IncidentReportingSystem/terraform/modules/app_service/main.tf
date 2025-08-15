data "azurerm_client_config" "current" {}

locals {
  merged_app_settings = var.app_settings
}


resource "azurerm_linux_web_app" "this" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = var.app_service_plan_id

  identity { type = "SystemAssigned" }

  site_config {
    application_stack { dotnet_version = "8.0" }
    always_on                         = var.always_on
    health_check_path                 = var.health_check_path
    health_check_eviction_time_in_min = var.health_check_eviction_time_in_min
    minimum_tls_version               = "1.2"
    ftps_state                        = "FtpsOnly"
  }

  https_only   = true
  app_settings = local.merged_app_settings
  tags         = var.tags
}

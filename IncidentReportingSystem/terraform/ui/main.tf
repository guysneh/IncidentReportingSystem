terraform {
  required_version = ">= 1.6.0"
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 3.116" }
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_service_plan" "plan" {
  name                = var.app_service_plan_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

locals {
  plan_id = data.azurerm_service_plan.plan.id
}

resource "azurerm_linux_web_app" "ui" {
  name                = var.ui_app_name
  location            = data.azurerm_resource_group.rg.location
  resource_group_name = data.azurerm_resource_group.rg.name
  service_plan_id     = local.plan_id
  https_only          = true

  site_config {
    application_stack { dotnet_version = "8.0" }
    always_on           = true
    http2_enabled       = true
    minimum_tls_version = "1.2"
    health_check_path   = "/"
  }

  app_settings = {
    ASPNETCORE_ENVIRONMENT           = "Production"
    Api__BaseUrl                     = var.api_base_url
    EnableHttpsRedirection           = "true"
    DataProtection__KeysDirectory    = "/home/keys"
  }
}

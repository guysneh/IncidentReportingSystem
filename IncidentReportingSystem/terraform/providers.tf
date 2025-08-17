terraform {
  required_version = ">= 1.6, < 2.0"
  required_providers {
    time = {
      source  = "hashicorp/time"
      version = "~> 0.9"
    }
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 4.30.0, < 5.0.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.7.0"
    }
    time = {
      source  = "hashicorp/time"
      version = "~> 0.13.0"
    }
  }
  backend "azurerm" {}
}

provider "azurerm" {
  features {
    app_configuration {
      # Disable soft-deleted pre-check/recovery. We handle purge ourselves if needed.
      recover_soft_deleted = false
    }
  }

  subscription_id = var.subscription_id
  tenant_id       = var.tenant_id
}


provider "azuread" {
  tenant_id = var.tenant_id
}

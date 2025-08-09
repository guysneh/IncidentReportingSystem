terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 3.89.0"
    }
  }

  required_version = ">= 1.4.0"
}

variable "subscription_id" {
  type        = string
  description = "Azure subscription ID"
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}


provider "azuread" {}


terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 3.50.0"
    }
  }

  required_version = ">= 1.5.0"
}

variable "subscription_id" {
  type        = string
  description = "Azure subscription ID"
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

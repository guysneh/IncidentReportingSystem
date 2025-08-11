terraform {
  required_version = ">= 1.6, < 2.0"

  required_providers {
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

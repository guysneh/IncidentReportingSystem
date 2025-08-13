provider "azurerm" {
  features {}
}

resource "random_string" "suffix" {
  length  = 6
  upper   = false
  special = false
}

resource "azurerm_resource_group" "tf" {
  name     = "incident-tfstate-rg"
  location = var.location
  tags     = var.default_tags
}

resource "azurerm_storage_account" "tf" {
  name                            = "incidenttfstate${random_string.suffix.result}"
  resource_group_name             = azurerm_resource_group.tf.name
  location                        = azurerm_resource_group.tf.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  allow_nested_items_to_be_public = false
  min_tls_version                 = "TLS1_2"
  allow_blob_public_access        = false
  tags                            = var.default_tags
}

resource "azurerm_storage_container" "tf" {
  name                  = "tfstate"
  storage_account_name  = azurerm_storage_account.tf.name
  container_access_type = "private"
}


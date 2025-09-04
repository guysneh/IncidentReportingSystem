locals {
  # Storage account name rules: 3–24 chars, lowercase letters + numbers only
  sa_name = substr(replace(lower(var.name), "/[^a-z0-9]/", ""), 0, 24)
}

resource "azurerm_storage_account" "this" {
  name                            = local.sa_name
  resource_group_name             = var.resource_group_name
  location                        = var.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  allow_nested_items_to_be_public = false
  min_tls_version                 = "TLS1_2"
  tags                            = var.default_tags
  blob_properties {
    cors_rule {
      allowed_headers    = ["*"]
      exposed_headers    = ["Content-Length", "Content-Type", "Content-Disposition", "ETag", "x-ms-*"]
      allowed_methods    = ["GET", "HEAD", "OPTIONS", "PUT", "POST", "DELETE"]
      allowed_origins    = ["https://localhost:5003"]
      max_age_in_seconds = 86400
    }
  }
}

resource "azurerm_storage_container" "attachments" {
  name                  = var.container_name
  storage_account_name  = azurerm_storage_account.this.name
  container_access_type = "private"
}

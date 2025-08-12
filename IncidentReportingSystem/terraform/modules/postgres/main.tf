# Generate password when not provided
resource "random_password" "pg_admin" {
  length  = 24
  special = true
}

# Choose provided password if any; otherwise the random one
locals {
  admin_password = coalesce(var.db_admin_password, random_password.pg_admin.result)
}

resource "azurerm_postgresql_flexible_server" "this" {
  name                = var.postgresql_server_name
  resource_group_name = var.resource_group_name
  location            = var.location

  administrator_login    = var.db_admin_username
  administrator_password = local.admin_password
  version                = "13"

  sku_name   = "GP_Standard_D2ds_v4"
  storage_mb = 32768

  backup_retention_days         = 7
  zone                          = "1"
  public_network_access_enabled = var.public_network_access_enabled
  tags                          = var.tags
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_all_azure" {
  count            = var.allow_all_azure ? 1 : 0
  name             = "AllowAllAzureIPs"
  server_id        = azurerm_postgresql_flexible_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Store admin password in Key Vault
resource "azurerm_key_vault_secret" "pg_admin_password" {
  name         = var.admin_password_secret_name
  value        = local.admin_password
  key_vault_id = var.key_vault_id
  content_type = "text/plain"
}

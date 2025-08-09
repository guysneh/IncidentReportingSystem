resource "azurerm_postgresql_flexible_server" "this" {
  name                   = var.postgresql_server_name
  resource_group_name    = var.resource_group_name
  location               = var.location
  administrator_login    = var.db_admin_username
  administrator_password = var.db_admin_password
  version                = "13"

  sku_name   = "GP_Standard_D2ds_v4"
  storage_mb = 32768

  backup_retention_days         = 7
  zone                          = "1"
  public_network_access_enabled = var.public_network_access_enabled
  tags                          = var.tags
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_all_azure" {
  name             = "AllowAllAzureIPs"
  server_id        = azurerm_postgresql_flexible_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
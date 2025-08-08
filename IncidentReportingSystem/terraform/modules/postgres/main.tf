resource "azurerm_postgresql_flexible_server" "this" {
  name                   = var.postgresql_server_name
  resource_group_name    = var.resource_group_name
  location               = var.location
  administrator_login    = var.db_admin_username
  administrator_password = var.db_admin_password
  version                = "13"

  sku_name   = "GP_Standard_D2ds_v4"
  storage_mb = 32768

  backup_retention_days = 7
  zone                  = "1"

  tags = var.tags
}

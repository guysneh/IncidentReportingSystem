output "server_id" { value = azurerm_postgresql_flexible_server.this.id }
output "server_name" { value = azurerm_postgresql_flexible_server.this.name }
output "server_fqdn" { value = azurerm_postgresql_flexible_server.this.fqdn }
output "admin_username" { value = var.db_admin_username }
output "admin_password" {
  value     = random_password.pg_admin.result
  sensitive = true
}

output "database_name" {
  value = var.database_name
}
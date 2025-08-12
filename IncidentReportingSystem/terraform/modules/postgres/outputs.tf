output "id" {
  value = azurerm_postgresql_flexible_server.this.id
}

output "fqdn" {
  value = azurerm_postgresql_flexible_server.this.fqdn
}

output "name" {
  value = azurerm_postgresql_flexible_server.this.name
}

output "server_name" { value = azurerm_postgresql_flexible_server.this.name }
output "server_fqdn" { value = azurerm_postgresql_flexible_server.this.fqdn }
output "admin_username" { value = var.db_admin_username }
output "admin_password_secret_id" { value = azurerm_key_vault_secret.pg_admin_password.id }

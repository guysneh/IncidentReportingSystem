resource "azurerm_key_vault_secret" "postgres_connection" {
  name         = "PostgreSqlConnectionString"
  key_vault_id = data.azurerm_key_vault.kv.id
  content_type = "text/plain"

  value = "Host=${module.postgres.server_fqdn};Database=${var.postgres_database};Username=${var.db_admin_username};Password=${module.postgres.admin_password};Port=${var.postgres_port};Ssl Mode=Require;Trust Server Certificate=true"
}


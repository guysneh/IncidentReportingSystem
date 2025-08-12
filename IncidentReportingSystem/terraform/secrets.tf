resource "azurerm_key_vault_secret" "postgres_connection" {
  name         = "PostgreSqlConnectionString"
  key_vault_id = module.key_vault.id

  value = format(
    "Host=%s;Port=%d;Database=%s;Username=%s;Password=%s;Ssl Mode=Require;Trust Server Certificate=true",
    module.postgres.server_fqdn,
    var.postgres_port,
    module.postgres.database_name,
    module.postgres.admin_username,
    module.postgres.admin_password
  )

  content_type = "text/plain"
}

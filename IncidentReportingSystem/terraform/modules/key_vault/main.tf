data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "this" {
  name                      = var.name
  location                  = var.location
  resource_group_name       = var.resource_group_name
  tenant_id                 = data.azurerm_client_config.current.tenant_id
  sku_name                  = "standard"
  enable_rbac_authorization = true
  purge_protection_enabled  = true

  soft_delete_retention_days = 7

  tags = var.tags

  lifecycle {
    ignore_changes = [soft_delete_retention_days]
  }
}

resource "azurerm_role_assignment" "ci_can_write_secrets" {
  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "time_sleep" "rbac_propagation" {
  depends_on      = [azurerm_role_assignment.ci_can_write_secrets]
  create_duration = "60s"
}

resource "azurerm_key_vault_secret" "secrets" {
  for_each     = var.secrets
  name         = each.key
  value        = each.value
  key_vault_id = azurerm_key_vault.this.id
  content_type = "text/plain"
  depends_on   = [time_sleep.rbac_propagation]
}
